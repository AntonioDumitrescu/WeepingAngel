# Remote Control - Weeping Angel

![Demo](https://github.com/AntonioDumitrescu/WeepingAngel/blob/master/demo0.png?raw=true)

## Setup

Pentru a folosi programul, citește [documentația](https://github.com/AntonioDumitrescu/WeepingAngel/blob/master/Setup.md).

## Descriere tehnică

### Comunicații
Este un program server-client, serverul fiind administratorul și clientul fiind un calculator administrat. 
Foloșeste un protocol creat de mine pe TCP. Are criptare cu AES-256 (cu o parolă predefinită si PBKDF2 pentru generarea cheii).
Foloșeste serializare binară și păstrează alocațiile.
Toate operațiile de I/O sunt asincrone și nu crează sau blochează threaduri.

### Arhitectură
Programul are o structură modulară, toată funcționalitatea fiind aflată în module, care pot fi create și de utilizatorii programului. 
Aceste module sunt încărcate la runtime de către server, și sunt trimise prin rețea la client și încărcate. Programul este construit cu o arhitectură de dependency injection,
ceea ce ușurează mult dezvoltarea modulelor, prin reducerea cuplării între dependențe și managementul automat al lifetime-ului serviciilor.
Logica pentru client/server se află în același proiect de C#, ceea ce reduce logistica necesară.

### GUI
Pentru interfața grafică, se folosește randare cu dispozitivul grafic și un back-end (OpenGL, Direct3D11) a unei interfețe generate dinamic (arborele de widgeturi este generat pe fiecare frame).
SDL a fost folosit în loc de Win32API. Am folosit bindings din ImGui.NET și Veldrid (libraries open-source).
Am considerat crearea propriul meu window system, OpenGl/DirectX bindings, si immediate mode GUI (ImGui), dar vreau să lucrez la alte proiecte.

Evenimentele de randare și input sunt trimise la module printr-un sistem de evenimente dinamic, care integrează dependency injection.

### Module incluse
Modulele incluse momentan:
	- Remote Terminal -> Command Prompt de la distanță
	- Remote Desktop -> vizualizarea ecranului de la distanță
		- Pentru codare video folosesc OpenH264 de la Cisco (proiect open-source) cu API de bindings și abstracție propriu (am ales să fac propriul API pentru că este mult mai puțină muncă decât sistemele grafice menționate mai sus).

### Teste
Proiectul include teste pentru părțile critice din sistemul de comunicație, criptare și serializare.

## Compilare
Soluția se compilează cu [Visual Studio 2022](https://visualstudio.microsoft.com/vs/), cu suport pentru .NET 6. Pentru a utiliza modulele incluse, trebuie create folderele `plugins` și `libraries` în directorul serverului.

## Biblioteci software
[OpenH264](https://github.com/cisco/openh264)
[ImGui.NET](https://github.com/mellinoe/ImGui.NET)
[Veldrid](https://github.com/mellinoe/veldrid/)

## API de plugins
Pentru a crea module, citește [documentația](https://github.com/AntonioDumitrescu/WeepingAngel/blob/master/API.md).
