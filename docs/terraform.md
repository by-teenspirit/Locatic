# Terraform 
Terraform est utilisé comme fournisseur de ressources d'infrastructure déclaratives pour Kubernetes (`provider "kubernetes"`). Il gère : 

- `kubernetes_namespace.locatic_infra` : Espace de noms logique isolant l'ensemble des ressources du TP pour éviter toute collision avec d'autres projets du cluster.
- `kubernetes_storage_class.local_storage` : Classe de stockage personnalisée définissant le comportement des disques virtuels, configurée avec `reclaim_policy = "Retain"` pour interdire l'effacement accidentel des données physiques en cas de suppression du PVC.
- `kubernetes_persistent_volume_claim.sqlite_pvc` : Demande d'allocation de stockage persistant d'une capacité de `1Gi`, requérant le mode d'accès `ReadWriteOnce`.

En local, le fichier `terraform.tfstate` est gardé localement à la racine du dossier `terraform/`. 
Ce fichier étant sensible, il est explicitement inscrit dans le fichier `.gitignore` pour de ne jamais être poussé sur le dépôt.

## Passerelle avec Ansible 
Pour éviter de dupliquer des informations en dur (comme le nom du Namespace ou du PVC) dans les configurations d'Ansible, Terraform expose ses résultats via des blocs `output`.

Ansible utilise ensuite la commande `terraform output -json` quand il s'initalise pour importer dynamiquement ces valeurs dans ses propres variables. 