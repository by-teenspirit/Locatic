variable "kube_context" {
  type        = string
  default     = "minikube"
  description = "Le contexte Kubernetes local à utiliser"
}

variable "namespace" {
  type        = string
  default     = "locatic-infra"
  description = "Namespace pour l'application Locatic"
}

variable "environment" {
  type        = string
  default     = "dev"
}