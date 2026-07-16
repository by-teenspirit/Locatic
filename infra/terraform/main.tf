terraform {
  required_version = ">= 1.6"
  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.26"
    }
  }
}

# Connexion automatique au contexte minikube local
provider "kubernetes" {
  config_path    = "~/.kube/config"
  config_context = var.kube_context
}

# 1. Création du Namespace
resource "kubernetes_namespace" "locatic_ns" {
  metadata {
    name = var.namespace
    labels = {
      environment = var.environment
      managed_by  = "terraform"
    }
  }
}

# 2. Création de la ConfigMap pour l'application (ex: Log level)
resource "kubernetes_config_map" "app_config" {
  metadata {
    name      = "locatic-config"
    namespace = kubernetes_namespace.locatic_ns.metadata[0].name
  }

  data = {
    NODE_ENV      = "production"
    APP_LOG_LEVEL = "debug"
  }
}

# 3. Création de la PVC pour la persistance SQLite (Contrainte majeure du sujet !)
resource "kubernetes_persistent_volume_claim" "sqlite_pvc" {
  metadata {
    name      = "sqlite-pvc"
    namespace = kubernetes_namespace.locatic_ns.metadata[0].name
  }
  spec {
    access_modes = ["ReadWriteOnce"]
    resources {
      requests = {
        storage = "1Gi"
      }
    }
  }
}