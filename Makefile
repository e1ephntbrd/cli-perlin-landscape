IMAGE_NAME = noise-landscape

.PHONY: help build run rebuild clean

help:
	@printf "\033[33mUsage:\033[0m\n  make [target] [arg=\"val\"...]\n\n\033[33mTargets:\033[0m\n"
	@grep -E '^[-a-zA-Z0-9_\.\/]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[32m%-15s\033[0m %s\n", $$1, $$2}'

build: ## Build project into a Docker image
	docker build -t $(IMAGE_NAME) .

run: ## Run the Docker container
	docker run -it --rm $(IMAGE_NAME)

rebuild: ## Rebuild the Docker image without using cache
	docker build --no-cache -t $(IMAGE_NAME) .

clean: ## Remove unused Docker images
	@docker image prune -f
	@echo "Temporary Docker images have been successfully removed."
