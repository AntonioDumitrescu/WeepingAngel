# Remote Control - Weeping Angel

![Demo](https://github.com/AntonioDumitrescu/WeepingAngel/blob/master/demo0.png?raw=true)

## Descriere tehnica

### Comunicatii
Este un program server-client, serverul fiind administratorul si clientul fiind un calculator administrat. 
Foloseste un protocol creat de mine pe TCP. Are criptare cu AES-256 (cu o parola predefinita si PBKDF2 pentru generarea cheii).
Foloseste serializare binara si se folosesc pools unde are sens, pentru a reduce alocarea.
Toate operatiile de I/O sunt asincrone si nu creaza sau blocheaza threaduri.

### Arhitectura
Programul are o structura modulara, toata functionalitatea fiind aflata in module, care pot fi create si de utilizatorii programului. 
Aceste module sunt incarcate la runtime de catre server, si sunt trimise prin retea si incarcate pentru client. Programul este construit cu o arhitectura de dependency injection,
ceea ce usureaza mult dezvoltarea modulelor, prin reductia cuplarii intre dependente si accesul usor la sistemele serverului/clientului sau altor module.
Logica pentru client/server se afla in acelasi modul, ceea ce reduce managementul necesar.

### GUI
Pentru interfata grafica, se foloseste randare cu dispozitivul grafic si un back-end (OpenGL, Direct3D11) a unei interfete generate dinamic (fara layout) numita Dear ImGui.
Am folosit SDL in loc de clasicul Win32API. Am folosit bindings si o abstractie grafica din ImGui.NET si Veldrid (libraries open-source, se poate gasi o lista
in repository). Am considerat să creez propriul meu window system, OpenGl/DirectX bindings, si immediate mode GUI, dar vreau să lucrez la alte proiecte.

Evenimentele de randare si input sunt trimise la module printr-un sistem de evenimente dinamic, care integreaza dependency injection si genereaza IL pentru functii la runtime.

### Module incluse
Modulele incluse momentan:
	- Remote Terminal -> Command Prompt de la distanta
	- Remote Desktop -> vizualizarea ecranului de la distanta
	
Pentru codare video folosesc OpenH264 de la Cisco (proiect open-source) cu API de bindings si abstractie propriu. Folosesc si un encoder/decoder de YUV420P propriu.

### Teste
Proiectul include teste pentru partile critice din sistemul de comunicatie, criptare, si serializare.

## Compilare

Solutia se compileaza cu [Visual Studio 2022](https://visualstudio.microsoft.com/vs/), cu suport pentru .NET 6. Pentru a utiliza modulele incluse, trebuie create folderele `plugins` si `libraries` in directorul serverului.

## Biblioteci software
[OpenH264](https://github.com/cisco/openh264)
[ImGui.NET](https://github.com/mellinoe/ImGui.NET)
[Veldrid](https://github.com/mellinoe/veldrid/)

## API de plugins
Pentru a crea module, citeste [documentatia](https://github.com/AntonioDumitrescu/WeepingAngel/blob/master/API.md).
