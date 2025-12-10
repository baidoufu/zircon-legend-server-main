#!/usr/bin/env bash
# 将镜像构建并推送到 Docker Hub，依赖 DOCKERHUB_USERNAME / DOCKERHUB_TOKEN 环境变量

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ -z "${DOCKERHUB_USERNAME:-}" || -z "${DOCKERHUB_TOKEN:-}" ]]; then
    echo "缺少 DOCKERHUB_USERNAME 或 DOCKERHUB_TOKEN 环境变量，无法推送。" >&2
    exit 1
fi

REGISTRY="${REGISTRY:-docker.io}"
IMAGE_NAME="${IMAGE_NAME:-zircon-legend-server}"
IMAGE_TAG="${IMAGE_TAG:-$(git -C "$repo_root" rev-parse --short HEAD 2>/dev/null || date +%Y%m%d%H%M)}"
FULL_IMAGE="${REGISTRY}/${DOCKERHUB_USERNAME}/${IMAGE_NAME}:${IMAGE_TAG}"

echo "==> 登录 ${REGISTRY}..."
echo "${DOCKERHUB_TOKEN}" | docker login -u "${DOCKERHUB_USERNAME}" --password-stdin "${REGISTRY}"

echo "==> 构建镜像 ${FULL_IMAGE} ..."
docker build -f "${repo_root}/Dockerfile" -t "${FULL_IMAGE}" "${repo_root}"

echo "==> 推送镜像 ${FULL_IMAGE} ..."
docker push "${FULL_IMAGE}"

if [[ "${PUSH_LATEST:-false}" == "true" ]]; then
    latest_tag="${REGISTRY}/${DOCKERHUB_USERNAME}/${IMAGE_NAME}:latest"
    echo "==> 同步 latest 标签 ${latest_tag} ..."
    docker tag "${FULL_IMAGE}" "${latest_tag}"
    docker push "${latest_tag}"
fi

echo "推送完成：${FULL_IMAGE}"
