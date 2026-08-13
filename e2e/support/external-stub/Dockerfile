# CommitAhead E2E external-stub — deterministic Node stdlib-only replacement for real Supabase
# Auth and Anthropic endpoints (E2E Foundation Plan). No dependencies to install, so this is a
# single-stage, single-file image.
FROM node:24-alpine
WORKDIR /app
COPY server.mjs /app/server.mjs
EXPOSE 8080
CMD ["node", "/app/server.mjs"]
