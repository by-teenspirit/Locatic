# Kubernetes - Passer de l'inventaire dynamique au runtime Kubernetes

## Objectif
Migrer l'application fil rouge des conteneurs Docker provisionnés par Terraform vers Kubernetes, en gardant Terraform comme source de vérité.

L'exercice 06 a rendu le lien Terraform → Ansible propre : Terraform générait l'inventaire, Ansible configurait les conteneurs Docker. Dans cet exercice, on change de runtime. Kubernetes remplace les conteneurs Docker pilotés un par un, et les informations qui passaient par l'inventaire Ansible deviennent des objets Kubernetes : `ConfigMap`, `Secret`, `Service`, `Deployment`.

À la fin de l'exercice, vous aurez :

- une image Docker `devops-app:1.0.0`
- un cluster Kubernetes local qui exécute l'application
- un namespace, une ConfigMap, un Secret, PostgreSQL, un Deployment applicatif et un Service créés par Terraform
- une preuve de fonctionnement via `kubectl` et `curl`
- une bascule contrôlée depuis l'ancien runtime Docker vers Kubernetes

## Contexte
Dans l'exercice 06, le flux était :

- Terraform provider Docker → conteneurs Docker locaux
- `terraform output -raw ansible_inventory` → inventaire Ansible généré
- Ansible → configuration des conteneurs existants

Maintenant, le flux devient :

- Terraform provider Kubernetes → objets Kubernetes
- `ConfigMap` / `Secret` → configuration applicative
- `Service` → nom réseau stable pour l'application et la base
- `Deployment` → nombre de replicas, probes et rolling updates

On ne génère donc plus d'inventaire Ansible pour les pods. Dans Kubernetes, les pods sont éphémères : on cible des labels, des Services et des Deployments, pas des noms de conteneurs écrits quelque part.

## Pré-requis

- Docker opérationnel
- `terraform`
- `kubectl`
- un cluster local `minikube` ou `kind`
- le contexte `kubectl` pointe sur le bon cluster
- l'exercice 06 terminé ou au moins compris
- l'exercice 06b terminé : image `devops-app:1.0.0` construite et chargée dans le cluster

Vérifier :

```bash
kubectl config current-context
kubectl get nodes
```

Vérifier aussi le point de départ côté exercice 06 :

```bash
cd infra/terraform/environments/dev
terraform output -raw ansible_inventory | head -40

cd ../../../ansible
./scripts/render-inventory.sh
ansible-inventory -i inventory.yml --graph
```

Ce contrôle sert uniquement à rappeler le flux précédent. Dans Kubernetes, on ne réutilisera pas cet inventaire.

Vérifier l'image préparée dans l'exercice 06b :

```bash
docker images devops-app:1.0.0
```

Pour `minikube` :

```bash
minikube image ls | grep devops-app
```

Pour `kind` :

```bash
docker exec devops-training-control-plane crictl images | grep devops-app
```

Si l'image n'est pas visible, faites l'exercice 06b avant de continuer.

## Consignes

### 1. Créer le dossier Terraform Kubernetes

```bash
mkdir -p infra/kubernetes/terraform
cd infra/kubernetes/terraform
```

### 2. Variables

Créer `variables.tf` :

```hcl
variable "kubeconfig_path" {
  type    = string
  default = "~/.kube/config"
}

variable "kube_context" {
  type        = string
  description = "Contexte kubectl à utiliser, par exemple minikube ou kind-devops-training."
  default     = "minikube"
}

variable "namespace" {
  type    = string
  default = "devops-training"
}

variable "app_name" {
  type    = string
  default = "devops-app"
}

variable "image_repository" {
  type    = string
  default = "devops-app"
}

variable "image_tag" {
  type    = string
  default = "1.0.0"
}

variable "app_port" {
  type    = number
  default = 3000
}

variable "app_replicas" {
  type    = number
  default = 3
}

variable "node_env" {
  type    = string
  default = "production"
}

variable "app_log_level" {
  type    = string
  default = "info"
}

variable "db_name" {
  type    = string
  default = "appdb"
}

variable "db_user" {
  type    = string
  default = "appuser"
}

variable "db_password" {
  type      = string
  sensitive = true
}

variable "postgres_storage" {
  type    = string
  default = "1Gi"
}
```

### 3. Ressources Kubernetes

Créer `main.tf` :

```hcl
terraform {
  required_version = ">= 1.5"
  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.35"
    }
  }
}

provider "kubernetes" {
  config_path    = pathexpand(var.kubeconfig_path)
  config_context = var.kube_context
}

locals {
  labels = {
    app        = var.app_name
    managed-by = "terraform"
  }
}

resource "kubernetes_namespace_v1" "main" {
  metadata {
    name = var.namespace
    labels = {
      project    = "devops-training"
      managed-by = "terraform"
    }
  }
}

resource "kubernetes_config_map_v1" "app" {
  metadata {
    name      = "app-config"
    namespace = kubernetes_namespace_v1.main.metadata[0].name
  }

  data = {
    NODE_ENV      = var.node_env
    APP_PORT      = tostring(var.app_port)
    DB_HOST       = "postgres-svc"
    DB_PORT       = "5432"
    DB_NAME       = var.db_name
    APP_LOG_LEVEL = var.app_log_level
    LOG_LEVEL     = var.app_log_level
  }
}

resource "kubernetes_secret_v1" "app" {
  metadata {
    name      = "app-secrets"
    namespace = kubernetes_namespace_v1.main.metadata[0].name
  }

  data = {
    DB_USER     = var.db_user
    DB_PASSWORD = var.db_password
  }

  type = "Opaque"
}

resource "kubernetes_persistent_volume_claim_v1" "postgres" {
  metadata {
    name      = "postgres-pvc"
    namespace = kubernetes_namespace_v1.main.metadata[0].name
    labels = {
      app = "postgres"
    }
  }

  spec {
    access_modes = ["ReadWriteOnce"]
    resources {
      requests = {
        storage = var.postgres_storage
      }
    }
  }
}

resource "kubernetes_deployment_v1" "postgres" {
  metadata {
    name      = "postgres"
    namespace = kubernetes_namespace_v1.main.metadata[0].name
    labels = {
      app = "postgres"
    }
  }

  spec {
    replicas = 1

    selector {
      match_labels = {
        app = "postgres"
      }
    }

    template {
      metadata {
        labels = {
          app = "postgres"
        }
      }

      spec {
        container {
          name  = "postgres"
          image = "postgres:16-alpine"

          port {
            name           = "postgres"
            container_port = 5432
          }

          env {
            name = "POSTGRES_DB"
            value_from {
              config_map_key_ref {
                name = kubernetes_config_map_v1.app.metadata[0].name
                key  = "DB_NAME"
              }
            }
          }

          env {
            name = "POSTGRES_USER"
            value_from {
              secret_key_ref {
                name = kubernetes_secret_v1.app.metadata[0].name
                key  = "DB_USER"
              }
            }
          }

          env {
            name = "POSTGRES_PASSWORD"
            value_from {
              secret_key_ref {
                name = kubernetes_secret_v1.app.metadata[0].name
                key  = "DB_PASSWORD"
              }
            }
          }

          volume_mount {
            name       = "postgres-storage"
            mount_path = "/var/lib/postgresql/data"
          }

          readiness_probe {
            exec {
              command = ["pg_isready", "-U", var.db_user]
            }
            initial_delay_seconds = 5
            period_seconds        = 5
          }
        }

        volume {
          name = "postgres-storage"
          persistent_volume_claim {
            claim_name = kubernetes_persistent_volume_claim_v1.postgres.metadata[0].name
          }
        }
      }
    }
  }
}

resource "kubernetes_service_v1" "postgres" {
  metadata {
    name      = "postgres-svc"
    namespace = kubernetes_namespace_v1.main.metadata[0].name
  }

  spec {
    selector = {
      app = "postgres"
    }

    port {
      name        = "postgres"
      port        = 5432
      target_port = "postgres"
    }

    type = "ClusterIP"
  }
}

resource "kubernetes_deployment_v1" "app" {
  metadata {
    name      = var.app_name
    namespace = kubernetes_namespace_v1.main.metadata[0].name
    labels    = local.labels
  }

  spec {
    replicas = var.app_replicas

    selector {
      match_labels = {
        app = var.app_name
      }
    }

    strategy {
      type = "RollingUpdate"
      rolling_update {
        max_surge       = "1"
        max_unavailable = "0"
      }
    }

    template {
      metadata {
        labels = local.labels
      }

      spec {
        container {
          name              = "app"
          image             = "${var.image_repository}:${var.image_tag}"
          image_pull_policy = "IfNotPresent"

          port {
            name           = "http"
            container_port = var.app_port
          }

          env_from {
            config_map_ref {
              name = kubernetes_config_map_v1.app.metadata[0].name
            }
          }

          env_from {
            secret_ref {
              name = kubernetes_secret_v1.app.metadata[0].name
            }
          }

          readiness_probe {
            http_get {
              path = "/health"
              port = var.app_port
            }
            initial_delay_seconds = 5
            period_seconds        = 10
          }

          liveness_probe {
            http_get {
              path = "/health"
              port = var.app_port
            }
            initial_delay_seconds = 15
            period_seconds        = 20
          }
        }
      }
    }
  }
}

resource "kubernetes_service_v1" "app" {
  metadata {
    name      = "${var.app_name}-svc"
    namespace = kubernetes_namespace_v1.main.metadata[0].name
  }

  spec {
    selector = {
      app = var.app_name
    }

    port {
      name        = "http"
      port        = 80
      target_port = "http"
    }

    type = "ClusterIP"
  }
}
```

### 4. Outputs

Créer `outputs.tf` :

```hcl
output "namespace" {
  value = kubernetes_namespace_v1.main.metadata[0].name
}

output "app_service" {
  value = kubernetes_service_v1.app.metadata[0].name
}

output "port_forward_command" {
  value = "kubectl port-forward -n ${kubernetes_namespace_v1.main.metadata[0].name} svc/${kubernetes_service_v1.app.metadata[0].name} 18080:80"
}

output "runtime_contract" {
  value = {
    runtime       = "kubernetes"
    replicas      = var.app_replicas
    app_log_level = var.app_log_level
    database_host = kubernetes_service_v1.postgres.metadata[0].name
  }
}
```

### 5. Valeurs d'environnement

Créer `terraform.tfvars` :

```hcl
kube_context  = "minikube"
namespace     = "devops-training"
image_tag     = "1.0.0"
app_replicas  = 3
app_log_level = "debug"
db_password   = "secret123"
```

`app_replicas = 3` et `app_log_level = "debug"` reprennent la preuve de l'exercice 06. Si votre groupe était resté à `info`, gardez `info`, mais gardez le même raisonnement : la configuration déclarée doit se retrouver dans le runtime.

Pour `kind`, adaptez le contexte :

```hcl
kube_context = "kind-devops-training"
```

> Point important : le mot de passe est ici volontairement simple pour le TP. En vrai, un Secret Terraform finit dans le state Terraform. Le state doit donc être protégé et chiffré.

### 6. Déployer avec Terraform

```bash
terraform init
terraform fmt
terraform plan
terraform apply
```

Vérifier :

```bash
terraform output
kubectl get all -n devops-training
kubectl wait --for=condition=available deployment/devops-app -n devops-training --timeout=120s
kubectl exec -n devops-training deploy/devops-app -- printenv | grep -E 'APP_LOG_LEVEL|LOG_LEVEL|DB_HOST'
```

### 7. Accéder à l'application

Utiliser le port `18080` pour éviter un conflit avec les anciens conteneurs Docker exposés sur `8080` et `8081` :

```bash
kubectl port-forward -n devops-training svc/devops-app-svc 18080:80
```

Dans un autre terminal :

```bash
curl http://localhost:18080/
curl http://localhost:18080/health
```

### 8. Basculer depuis l'ancienne version Docker/Ansible

Pendant quelques minutes, les deux versions peuvent coexister :

- ancienne version : conteneurs Docker de l'exercice 06, configurés par Ansible avec l'inventaire généré
- nouvelle version : pods Kubernetes exposés via le Service `devops-app-svc`

Quand la version Kubernetes fonctionne, vous pouvez arrêter l'infrastructure Docker de l'exercice 06 :

```bash
cd ../../terraform/environments/dev
terraform destroy -var-file="terraform.tfvars" -var="db_password=secret123"
```

Cela détruit uniquement l'ancien runtime Docker. Le déploiement Kubernetes vit dans un autre dossier Terraform et reste disponible.

Revenir ensuite au dossier Kubernetes :

```bash
cd ../../../kubernetes/terraform
```

L'application reste disponible via Kubernetes :

```bash
curl http://localhost:18080/health
```

### 9. Scaling déclaratif

Changer `app_replicas` dans `terraform.tfvars` :

```hcl
app_replicas = 5
```

Puis appliquer :

```bash
terraform plan
terraform apply
kubectl get pods -n devops-training -l app=devops-app
```

Comparez avec :

```bash
kubectl scale deployment devops-app --replicas=2 -n devops-training
terraform plan
```

Terraform doit détecter un drift : l'état réel du cluster ne correspond plus au code.

### 10. Rolling update déclaratif

Construire une nouvelle image :

```bash
docker build -t devops-app:1.0.1 starter-code/app
minikube image load devops-app:1.0.1
# ou : kind load docker-image devops-app:1.0.1 --name devops-training
```

Changer `image_tag` :

```hcl
image_tag = "1.0.1"
```

Appliquer :

```bash
terraform apply
kubectl rollout status deployment/devops-app -n devops-training
kubectl rollout history deployment/devops-app -n devops-training
```

## Livrable

- Dossier `infra/kubernetes/terraform`
- Déploiement Terraform réussi sur Kubernetes
- Namespace `devops-training`
- Deployment `devops-app` avec 3 replicas, probes et Service
- PostgreSQL avec PVC et Service interne
- Preuve `curl /` et `curl /health`
- Preuve que `app_log_level` est passé dans la ConfigMap Kubernetes
- Démonstration d'un scaling via Terraform
- Explication du drift après un changement manuel `kubectl`

## Aide

### Commandes utiles

```bash
kubectl get all -n devops-training
kubectl describe pod -n devops-training <pod>
kubectl logs -n devops-training -l app=devops-app --tail=50
kubectl get events -n devops-training --sort-by=.metadata.creationTimestamp
kubectl exec -n devops-training deploy/devops-app -- printenv | sort
kubectl get configmap app-config -n devops-training -o yaml
```

### Si l'image ne démarre pas

Vérifiez que l'image est bien présente dans le cluster :

```bash
# minikube
minikube image ls | grep devops-app

# kind
docker exec -it devops-training-control-plane crictl images | grep devops-app
```

### Nettoyage

```bash
terraform destroy
```

Si le namespace reste bloqué :

```bash
kubectl delete namespace devops-training
```
