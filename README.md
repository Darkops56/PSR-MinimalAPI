<h1 align="center">E.T. Nº12 D.E. 1º "Libertador Gral. José de San Martín"</h1>
<p align="center">
  <img src="https://et12.edu.ar/imgs/computacion/vamoaprogramabanner.png">
</p>

# Ejemplo MinimalAPI

Despliegue de API TODO basado en el ejemplo de _minimal API_ de MSDN.


## Pre-requisitos 📋

- SDK .NET 8
- SDK GoLang 1.24.4
- Visual Studio Code

## Comenzando 🚀

Clonar el repositorio github, desde Github Desktop o ejecutar en la terminal o CMD:

```
git clone https://github.com/luchoxx87/PSR-MinimalAPI
```

## Despliegue 📦

1. Abrir la terminal en el directorio donde están los scripts, entrar al directorio `TodoPSR` y ejecutar el comando `dotnet run`. Esto va a dejar en una terminal corriendo el servicio de tu _API REST_.
1. Podemos ver la documentación y consumir nuestra _API REST_, ingresando a http://localhost:5250/scalar/
1. De vuelta en el directorio raíz, ejecutar el comando `go run consumo.go`. Esto va a compilar y ejecutar el script de golang, el cual va a consumir nuestra _API REST_ consumiendo el _endpoint_ `/todoitems` mediante _GET_.
1. Si para el paso anterior, previamente agregamos _TODOs_ mediante el _endpoint_ `/todoitems` y _POST_, deberíamos verlos en la salida de la consola de la aplicación Golang.