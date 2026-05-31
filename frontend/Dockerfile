# -------------------------------------------------------
# ETAP 1: Build
# -------------------------------------------------------
FROM node:20-alpine AS build
WORKDIR /app

# Kopiujemy package.json - bez package-lock.json, npm install wygeneruje go świeżo
COPY package.json ./
RUN npm install

# Kopiujemy kod i budujemy
COPY . .

ARG REACT_APP_API_URL=http://localhost:5000
ENV REACT_APP_API_URL=$REACT_APP_API_URL

RUN npm run build

# -------------------------------------------------------
# ETAP 2: Serve przez nginx
# -------------------------------------------------------
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
RUN rm -rf ./*
COPY --from=build /app/build .
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
