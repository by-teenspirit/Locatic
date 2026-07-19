# Déploiement local

On applique Terraform avant Ansible car Ansible a besoin que le namespace et le PVC existent pour déployer l'application. 

Flux d'Installation : 
    Minikube local -> Terraform (Socle K8s) -> Ansible (Déploiement) -> Local Hosts (Routage)

### Commandes à lancer 
Activer Minikube et l'Ingress 
```bash
minikube start
minikube addons enable ingress
```

Initialiser et appliquer Terraform
- Se placer dans le dossier /terraform, initialiser et appliquer Terraform
```bash
cd infra/terraform
terraform init
terraform apply -auto-approve
```

Lancer le playbook Ansible 
```bash
cd ..
cd ansible
ansible-playbook -i inventory.yml site.yml
cd ..
```

Récupérer l'ip du Minikube 
- ```minikube ip```
- Copier l'IP 
- Ajouter ceci au fichier ```/etc/hosts``` avec la commande ```sudo nano /etc/hosts```
- Coller "ip copiée" locatic.local
Cela devrait ressembler à ```155.165.49.2 locatic.local```
- Sauvegarder et quitter (Ctrl+O, Entrée, puis Ctrl+X).
- Valider et se rendre sur http://locatic.local