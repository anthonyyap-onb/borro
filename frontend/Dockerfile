FROM node:18-alpine AS dev

WORKDIR /app

# Install dependencies first (cached layer)
COPY package.json package-lock.json* ./
RUN npm install --legacy-peer-deps

# Copy source
COPY . .

# The VITE_API_URL is injected via docker-compose environment variable at runtime
# Vite picks it up because env vars are inlined at build time for `npm run dev`
EXPOSE 5173

CMD ["npm", "run", "dev"]
