FROM node:22-alpine

WORKDIR /app

COPY src/InsightVault.Client/package*.json ./
RUN npm ci

COPY src/InsightVault.Client/ ./

ENV VITE_API_BASE_URL=http://localhost:5089

EXPOSE 56772

CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]
