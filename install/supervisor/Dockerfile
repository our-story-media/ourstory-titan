FROM node:12-stretch

WORKDIR /build

RUN npm config set unsafe-perm true && npm install -g pkg

COPY package.json .

RUN npm i --silent

COPY . .

RUN pkg indaba-supervisor.js --targets armv7