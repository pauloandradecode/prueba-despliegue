# MS Encrypt RSA algorithm 

Microservicio para la creación de llave publica y privada para la encriptación con el algoritmo RSA 

## Instalación de nugets

Para que el proyecto funcione se deben instalar los siguientes nuggets

```
dotnet add MS_Emcrypt_RSA_Algorithm package MongoDB.Driver --version 2.27.0
dotnet add MS_Emcrypt_RSA_Algorithm.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add MS_Emcrypt_RSA_Algorithm package MiniValidation
dotnet add MS_Emcrypt_RSA_Algorithm package Swashbuckle.AspNetCore --version 6.6.2
dotnet add MS_Emcrypt_RSA_Algorithm package Microsoft.AspNetCore.OpenApi --version 8.0.7
```


## Ejecución del proyecto

Para correr el proyecto, ejecute el siguiente comando:

```
dotnet run --project MS_Emcrypt_RSA_Algorithm/MS_Emcrypt_RSA_Algorithm.csproj --launch-profile "Development"
```

## Instalación de nugets para pruebas

Para que el proyecto de pruebas funcione se deben instalar los siguientes nuggets

```
dotnet add MS_Emcrypt_RSA_Algorithm.Tests package Moq --version 4.20.70
dotnet add MS_Emcrypt_RSA_Algorithm.Tests package MongoDB.Driver --version 2.27.0
```