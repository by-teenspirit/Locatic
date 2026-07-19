# Ansible

Le playbook Ansible (`site.yml`) permet d'automatiser l'étape finale du déploiement. 
Il traduit les configurations logiques en objets Kubernetes réels. 

## Étapes 
1.  **Collecte des Faits Terraform :** Exécute une routine pour extraire les `outputs` du dossier Terraform pour garantir la synchronisation des environnements.

2.  **Génération des Fichiers de Configuration (Templating Jinja2) :** Lit les fichiers sources `app-deployment.yml.j2` et `app-ingress.yml.j2`. Ansible remplace dynamiquement les expressions telles que `{{ kubernetes_namespace }}` ou `{{ image_tag }}` par les valeurs réelles calculées.

3.  **Application sur le Cluster :** Utilise le module natif `kubernetes.core.k8s` pour envoyer les fichiers générés à l'API Server de Kubernetes, assurant un état convergent.