#!/usr/bin/env bash
dotnet publish -c release -r linux-x64
docker build .
