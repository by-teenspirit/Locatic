# 06b - Image Docker pour Kubernetes


## Contexte

Avant de migrer vers Kubernetes, il faut figer l'application sous forme d'image. Kubernetes ne construit pas l'image pour nous, il consomme une image déjà disponible dans son runtime.


## Pré-requis

- Docker opérationnel
- le dossier `starter-code/app` (ou le nom que vous lui avez déjà donné)
- un cluster local `minikube` ou `kind` déjà créé

Vérifier :

```bash
docker version
kubectl config current-context
kubectl get nodes
```

## Consignes

### 1. Lire le Dockerfile du fil rouge

Depuis la racine du dépôt :

```bash
sed -n '1,120p' starter-code/app/Dockerfile
```

Repérer :

- le stage `test`, qui exécute `npm test`
- le stage `production`, qui installe uniquement les dépendances runtime
- `USER node`
- `EXPOSE 3000`
- le `HEALTHCHECK` sur `/health`

### 2. Construire l'image

```bash
docker build -t devops-app:1.0.0 starter-code/app
```

Vérifier :

```bash
docker images devops-app
docker image inspect devops-app:1.0.0 --format '{{.Config.User}} {{.Config.ExposedPorts}}'
```

Résultat attendu :

- l'image `devops-app:1.0.0` existe
- l'utilisateur runtime est `node`
- le port exposé est `3000/tcp`

### 3. Tester l'image avant Kubernetes

Lancer l'application en local :

```bash
docker run --rm -d \
  --name devops-app-image-check \
  -p 18080:3000 \
  devops-app:1.0.0
```

Vérifier :

```bash
curl http://localhost:18080/
curl http://localhost:18080/health
docker logs devops-app-image-check --tail=20
```

Arrêter le conteneur :

```bash
docker stop devops-app-image-check
```

### 4. Charger l'image dans le cluster local

Si vous utilisez `minikube` :

```bash
minikube image load devops-app:1.0.0
minikube image ls | grep devops-app
```

Si vous utilisez `kind` :

```bash
kind load docker-image devops-app:1.0.0 --name devops-training
docker exec devops-training-control-plane crictl images | grep devops-app
```

> Adaptez `devops-training` si votre cluster kind a un autre nom.

### 5. Résumer le contrat pour Kubernetes

Noter les informations que le Terraform Kubernetes de l'exercice 07 devra réutiliser :

```text
image_repository = "devops-app"
image_tag        = "1.0.0"
app_port         = 3000
```

## Livrable

- Image locale `devops-app:1.0.0`
- Test `curl /health` réussi depuis un conteneur Docker local
- Image chargée dans `minikube` ou `kind`
- Explication courte : pourquoi Kubernetes a besoin que l'image soit visible par le cluster

## Aide

### Si Kubernetes ne trouve pas l'image

Symptôme probable dans l'exercice 07 :

```bash
kubectl get pods -n devops-training
kubectl describe pod -n devops-training <pod>
```

Si vous voyez `ImagePullBackOff` ou `ErrImagePull`, rechargez l'image dans le cluster :

```bash
minikube image load devops-app:1.0.0
# ou
kind load docker-image devops-app:1.0.0 --name devops-training
```

### Nettoyage optionnel

```bash
docker image rm devops-app:1.0.0
```
