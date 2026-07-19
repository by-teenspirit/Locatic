# Projet Locatic

## Objectif 
Locatic est une application web moderne développée en **C# / .NET 8**. Ce dépôt contient l'intégralité de la chaîne d'ingénierie DevOps permettant d'assurer la validation de la qualité du code, sa sécurisation contre les vulnérabilités, sa conteneurisation, ainsi que son déploiement automatisé et résilient sur un cluster **Kubernetes (Minikube)** local via un couplage **Terraform ➡️ Ansible**.

## Structure du dépôt 
```text
├── .github/workflows/   # Pipeline CI/CD GitHub Actions
├── app/                 # Code source de l'application .NET 8 & Dockerfile
├── terraform/           # Définition de l'infrastructure de base (Namespace, Storage)
├── ansible/             # Orchestration et déploiement des manifestes Kubernetes
└── docs/                # Documentation technique détaillée
```

## Pré-requis 
- Minikube & Desktop Docker installé et démarré 
- Addon Ingress activé sur Minikube 
- Terraform (>= 1.6) et Ansible installés localement avec la collection kubernetes.core
- Kubectl

## Documentation
- docs/ansible.md
- docs/architecture.md 
- docs/ci-cd.md
- docs/deploiement-local.md
- docs/exploitation.md
- docs/helm.md
- docs/kubernetes.md
- docs/monitoring.md
- docs/terraform.md