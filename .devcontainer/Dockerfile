FROM mcr.microsoft.com/devcontainers/python:1-3.12-bullseye

# Fix: Yarn apt repo signature breaks apt-get update (EXPKEYSIG ...)
RUN rm -f /etc/apt/sources.list.d/yarn.list /etc/apt/sources.list.d/yarn*.list \
 && rm -f /usr/share/keyrings/yarn*.gpg /etc/apt/trusted.gpg.d/yarn*.gpg \
 && apt-get update
