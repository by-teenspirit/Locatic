# Kubernetes

Le cluster a une architecture composée en trois piliers interconnectés. Ainsi, on assure la haute disponibilités, l'accès au réseau et la persistance. 

## Déploiement
Le fichier `app-deployment.yml` configure les caractéristiques d'exécution des conteneurs :

*   **Image :** Récupérée depuis GHCR (`ghcr.io/by-teenspirit/locatic-app:main`)

*   **Healthchecks :** Pour s'assurer que le serveur web .NET 8 est totalement prêt à recevoir du trafic avant de lui envoyer des utilisateurs, des sondes de disponibilité 
(`readinessProbe`) et de vie (`livenessProbe`) interrogent l'application sur son port interne `3000`

*   **Montage du Volume :** Le conteneur possède une section `volumeMounts` mappant le volume externe sur le chemin absolu `/app/data` (dossier où l'application écrit son fichier SQLite).

## Service
On instancie un objet `Service` pour répartir la charge interne du cluster afin qu'elle reste stable. Il expose le port `80` et redirige le trafic vers le port `3000` avec des pods actifs étant flaggé comme `app: locatic-app`

## Ingress
Il mappe les requêtes HTTP externes qui arrivent. Il les redirige vers le `Service`