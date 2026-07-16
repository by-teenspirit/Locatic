# Helm : Packager le runtime Kubernetes et piloter la release avec Terraform

## Objectif
Transformer le déploiement Kubernetes de l'exercice 07 en chart Helm, puis gérer la release soit avec Helm CLI, soit avec Terraform via le provider Helm.

L'objectif est de garder le même contrat runtime que dans le 07 :

- `app_replicas` devient `replicaCount`
- `app_log_level` devient `config.appLogLevel`
- `database_host` reste le Service DNS PostgreSQL
- ConfigMap, Secret, Deployment, Service et probes restent présents, mais packagés dans un chart

## Contexte
L'exercice 07 a montré que Terraform peut créer directement des ressources Kubernetes. C'est utile pour comprendre les objets, mais cela devient vite verbeux.

La progression est maintenant :

```text
06 : Terraform output -> inventaire Ansible -> conteneurs Docker
06b: Docker build -> image devops-app:1.0.0 disponible dans le cluster
07 : Terraform Kubernetes provider -> objets Kubernetes
08 : Helm chart -> release Kubernetes
```

Helm ajoute une couche de packaging :

- les manifests Kubernetes deviennent des templates
- les différences entre environnements vivent dans des fichiers `values`
- le contrat runtime devient une interface claire du chart
- les upgrades et rollbacks sont gérés comme des releases

Terraform peut ensuite piloter Helm avec `helm_release`, ce qui donne :

```text
Terraform → Helm release → Kubernetes resources
```

## Pré-requis

- Exercice 07 compris
- Cluster Kubernetes local opérationnel
- Exercice 06b terminé : image `devops-app:1.0.0` chargée dans le cluster
- `helm`
- `terraform`

Si l'exercice 07 tourne encore, utilisez un namespace différent ici (`devops-helm`) pour éviter les conflits.

## Consignes

### 1. Créer la structure Helm

```bash
mkdir -p infra/helm/devops-app-chart/templates
cd infra/helm
```

Structure attendue :

```text
infra/helm/
  devops-app-chart/
    Chart.yaml
    values.yaml
    templates/
      _helpers.tpl
      configmap.yaml
      secret.yaml
      postgres.yaml
      deployment.yaml
      service.yaml
      NOTES.txt
  values-dev.yaml
  values-prod.yaml
```

### 2. Métadonnées du chart

Créer `devops-app-chart/Chart.yaml` :

```yaml
apiVersion: v2
name: devops-app
description: Application de démo DevOps packagée avec Helm
type: application
version: 0.1.0
appVersion: "1.0.0"
maintainers:
  - name: DevOps Team
```

### 3. Values par défaut

Créer `devops-app-chart/values.yaml` :

```yaml
replicaCount: 3

image:
  repository: devops-app
  tag: "1.0.0"
  pullPolicy: IfNotPresent

service:
  type: ClusterIP
  port: 80

config:
  nodeEnv: production
  appLogLevel: info
  appPort: 3000
  dbHost: postgres-svc
  dbPort: "5432"
  dbName: appdb

secret:
  dbUser: appuser
  dbPassword: ""

postgresql:
  enabled: true
  image: postgres:16-alpine
  storage: 1Gi

resources:
  requests:
    cpu: 100m
    memory: 128Mi
  limits:
    cpu: 250m
    memory: 256Mi

probes:
  readiness:
    path: /health
    initialDelaySeconds: 5
    periodSeconds: 10
  liveness:
    path: /health
    initialDelaySeconds: 15
    periodSeconds: 20
```

### 4. Helpers

Créer `devops-app-chart/templates/_helpers.tpl` :

```gotemplate
{{- define "devops-app.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "devops-app.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- if contains $name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}
{{- end -}}

{{- define "devops-app.labels" -}}
app.kubernetes.io/name: {{ include "devops-app.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version | replace "+" "_" }}
{{- end -}}

{{- define "devops-app.selectorLabels" -}}
app.kubernetes.io/name: {{ include "devops-app.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end -}}
```

### 5. ConfigMap et Secret

Créer `devops-app-chart/templates/configmap.yaml` :

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: {{ include "devops-app.fullname" . }}-config
  labels:
    {{- include "devops-app.labels" . | nindent 4 }}
data:
  NODE_ENV: {{ .Values.config.nodeEnv | quote }}
  APP_PORT: {{ .Values.config.appPort | quote }}
  DB_HOST: {{ .Values.config.dbHost | quote }}
  DB_PORT: {{ .Values.config.dbPort | quote }}
  DB_NAME: {{ .Values.config.dbName | quote }}
  APP_LOG_LEVEL: {{ .Values.config.appLogLevel | quote }}
  LOG_LEVEL: {{ .Values.config.appLogLevel | quote }}
```

Créer `devops-app-chart/templates/secret.yaml` :

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: {{ include "devops-app.fullname" . }}-secrets
  labels:
    {{- include "devops-app.labels" . | nindent 4 }}
type: Opaque
stringData:
  DB_USER: {{ .Values.secret.dbUser | quote }}
  DB_PASSWORD: {{ required "secret.dbPassword is required" .Values.secret.dbPassword | quote }}
```

> Le `required` force à fournir un mot de passe au moment de l'installation.

### 6. PostgreSQL

Créer `devops-app-chart/templates/postgres.yaml` :

```yaml
{{- if .Values.postgresql.enabled }}
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: {{ include "devops-app.fullname" . }}-postgres-pvc
  labels:
    {{- include "devops-app.labels" . | nindent 4 }}
    app.kubernetes.io/component: postgres
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: {{ .Values.postgresql.storage }}
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ include "devops-app.fullname" . }}-postgres
  labels:
    {{- include "devops-app.labels" . | nindent 4 }}
    app.kubernetes.io/component: postgres
spec:
  replicas: 1
  selector:
    matchLabels:
      {{- include "devops-app.selectorLabels" . | nindent 6 }}
      app.kubernetes.io/component: postgres
  template:
    metadata:
      labels:
        {{- include "devops-app.selectorLabels" . | nindent 8 }}
        app.kubernetes.io/component: postgres
    spec:
      containers:
        - name: postgres
          image: {{ .Values.postgresql.image | quote }}
          ports:
            - name: postgres
              containerPort: 5432
          env:
            - name: POSTGRES_DB
              valueFrom:
                configMapKeyRef:
                  name: {{ include "devops-app.fullname" . }}-config
                  key: DB_NAME
            - name: POSTGRES_USER
              valueFrom:
                secretKeyRef:
                  name: {{ include "devops-app.fullname" . }}-secrets
                  key: DB_USER
            - name: POSTGRES_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: {{ include "devops-app.fullname" . }}-secrets
                  key: DB_PASSWORD
          readinessProbe:
            exec:
              command: ["pg_isready", "-U", {{ .Values.secret.dbUser | quote }}]
            initialDelaySeconds: 5
            periodSeconds: 5
          volumeMounts:
            - name: postgres-storage
              mountPath: /var/lib/postgresql/data
      volumes:
        - name: postgres-storage
          persistentVolumeClaim:
            claimName: {{ include "devops-app.fullname" . }}-postgres-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: postgres-svc
  labels:
    {{- include "devops-app.labels" . | nindent 4 }}
    app.kubernetes.io/component: postgres
spec:
  type: ClusterIP
  selector:
    {{- include "devops-app.selectorLabels" . | nindent 4 }}
    app.kubernetes.io/component: postgres
  ports:
    - name: postgres
      port: 5432
      targetPort: postgres
{{- end }}
```

### 7. Application

Créer `devops-app-chart/templates/deployment.yaml` :

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ include "devops-app.fullname" . }}
  labels:
    {{- include "devops-app.labels" . | nindent 4 }}
spec:
  replicas: {{ .Values.replicaCount }}
  selector:
    matchLabels:
      {{- include "devops-app.selectorLabels" . | nindent 6 }}
      app.kubernetes.io/component: app
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  template:
    metadata:
      labels:
        {{- include "devops-app.selectorLabels" . | nindent 8 }}
        app.kubernetes.io/component: app
    spec:
      containers:
        - name: app
          image: "{{ .Values.image.repository }}:{{ .Values.image.tag }}"
          imagePullPolicy: {{ .Values.image.pullPolicy }}
          ports:
            - name: http
              containerPort: {{ .Values.config.appPort }}
          envFrom:
            - configMapRef:
                name: {{ include "devops-app.fullname" . }}-config
            - secretRef:
                name: {{ include "devops-app.fullname" . }}-secrets
          readinessProbe:
            httpGet:
              path: {{ .Values.probes.readiness.path }}
              port: {{ .Values.config.appPort }}
            initialDelaySeconds: {{ .Values.probes.readiness.initialDelaySeconds }}
            periodSeconds: {{ .Values.probes.readiness.periodSeconds }}
          livenessProbe:
            httpGet:
              path: {{ .Values.probes.liveness.path }}
              port: {{ .Values.config.appPort }}
            initialDelaySeconds: {{ .Values.probes.liveness.initialDelaySeconds }}
            periodSeconds: {{ .Values.probes.liveness.periodSeconds }}
          resources:
            {{- toYaml .Values.resources | nindent 12 }}
```

Créer `devops-app-chart/templates/service.yaml` :

```yaml
apiVersion: v1
kind: Service
metadata:
  name: {{ include "devops-app.fullname" . }}-svc
  labels:
    {{- include "devops-app.labels" . | nindent 4 }}
spec:
  type: {{ .Values.service.type }}
  selector:
    {{- include "devops-app.selectorLabels" . | nindent 4 }}
    app.kubernetes.io/component: app
  ports:
    - name: http
      port: {{ .Values.service.port }}
      targetPort: http
```

Créer `devops-app-chart/templates/NOTES.txt` :

```gotemplate
Application installée.

Port-forward :
  kubectl port-forward -n {{ .Release.Namespace }} svc/{{ include "devops-app.fullname" . }}-svc 18080:{{ .Values.service.port }}

Vérification :
  curl http://localhost:18080/
  curl http://localhost:18080/health
```

### 8. Values par environnement

Créer `values-dev.yaml` :

```yaml
replicaCount: 1

image:
  tag: "1.0.0"

config:
  nodeEnv: development
  appLogLevel: debug

resources:
  requests:
    cpu: 50m
    memory: 64Mi
  limits:
    cpu: 100m
    memory: 128Mi
```

Créer `values-prod.yaml` :

```yaml
replicaCount: 5

config:
  nodeEnv: production
  appLogLevel: warn

resources:
  requests:
    cpu: 250m
    memory: 256Mi
  limits:
    cpu: 500m
    memory: 512Mi
```

### 9. Installer avec Helm CLI

```bash
helm lint devops-app-chart --set secret.dbPassword=secret123

helm template devops-app devops-app-chart \
  -f values-dev.yaml \
  --set secret.dbPassword=secret123

helm upgrade --install devops-app devops-app-chart \
  -n devops-helm \
  --create-namespace \
  -f values-dev.yaml \
  --set secret.dbPassword=secret123
```

Vérifier :

```bash
helm list -n devops-helm
kubectl get all -n devops-helm
kubectl rollout status deployment/devops-app -n devops-helm
kubectl exec -n devops-helm deploy/devops-app -- printenv | grep -E 'APP_LOG_LEVEL|LOG_LEVEL|DB_HOST'
kubectl port-forward -n devops-helm svc/devops-app-svc 18080:80
```

Dans un autre terminal :

```bash
curl http://localhost:18080/
curl http://localhost:18080/health
```

### 10. Upgrade et rollback

Passer en configuration prod :

```bash
helm upgrade devops-app devops-app-chart \
  -n devops-helm \
  -f values-prod.yaml \
  --set secret.dbPassword=secret123

helm history devops-app -n devops-helm
kubectl get pods -n devops-helm
```

Rollback :

```bash
helm rollback devops-app 1 -n devops-helm
helm history devops-app -n devops-helm
```

### 11. Option : gérer Helm avec Terraform

Ne gardez pas la même release gérée à la fois par Helm CLI et Terraform. Pour basculer proprement :

```bash
helm uninstall devops-app -n devops-helm
```

Créer `terraform/main.tf` :

```hcl
terraform {
  required_version = ">= 1.5"
  required_providers {
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.17"
    }
  }
}

provider "helm" {
  kubernetes {
    config_path    = pathexpand(var.kubeconfig_path)
    config_context = var.kube_context
  }
}

resource "helm_release" "devops_app" {
  name             = "devops-app"
  chart            = "${path.module}/../devops-app-chart"
  namespace        = var.namespace
  create_namespace = true

  values = [
    file("${path.module}/../values-dev.yaml")
  ]

  set {
    name  = "image.tag"
    value = var.image_tag
  }

  set {
    name  = "replicaCount"
    value = var.app_replicas
  }

  set {
    name  = "config.appLogLevel"
    value = var.app_log_level
  }

  set_sensitive {
    name  = "secret.dbPassword"
    value = var.db_password
  }
}

output "port_forward_command" {
  value = "kubectl port-forward -n ${var.namespace} svc/devops-app-svc 18080:80"
}

output "runtime_contract" {
  value = {
    runtime       = "kubernetes-helm"
    release       = helm_release.devops_app.name
    namespace     = var.namespace
    replicas      = var.app_replicas
    app_log_level = var.app_log_level
    database_host = "postgres-svc"
  }
}
```

Créer `terraform/variables.tf` :

```hcl
variable "kubeconfig_path" {
  type    = string
  default = "~/.kube/config"
}

variable "kube_context" {
  type    = string
  default = "minikube"
}

variable "namespace" {
  type    = string
  default = "devops-helm"
}

variable "image_tag" {
  type    = string
  default = "1.0.0"
}

variable "app_replicas" {
  type    = number
  default = 1
}

variable "app_log_level" {
  type    = string
  default = "debug"
}

variable "db_password" {
  type      = string
  sensitive = true
}
```

Créer `terraform/terraform.tfvars` :

```hcl
kube_context  = "minikube"
namespace     = "devops-helm"
image_tag     = "1.0.0"
app_replicas  = 1
app_log_level = "debug"
db_password   = "secret123"
```

Déployer :

```bash
cd terraform
terraform init
terraform plan
terraform apply
terraform output
```

> `set_sensitive` évite d'afficher le mot de passe dans les logs Terraform, mais le state Terraform reste sensible et doit être protégé.

## Livrable

- Chart Helm `devops-app-chart`
- `values-dev.yaml` et `values-prod.yaml`
- Installation Helm réussie dans `devops-helm`
- Upgrade et rollback démontrés
- Option Terraform `helm_release` fonctionnelle
- Preuve que `appLogLevel` arrive dans la ConfigMap générée
- Explication claire : Terraform Kubernetes provider vs Terraform Helm provider

## Aide

### Debug Helm

```bash
helm template devops-app devops-app-chart --debug --set secret.dbPassword=secret123
helm install devops-app devops-app-chart --dry-run --debug --set secret.dbPassword=secret123
helm get values devops-app -n devops-helm
helm get manifest devops-app -n devops-helm
```

### Nettoyage

Si installé avec Helm CLI :

```bash
helm uninstall devops-app -n devops-helm
kubectl delete namespace devops-helm
```

Si installé avec Terraform :

```bash
cd terraform
terraform destroy
```
