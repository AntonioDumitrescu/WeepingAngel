# Instalare

Nu este necesară o instalare specială. Singurul setup obligatoriu este configurația de conectare.
Pentru a obține un șablon, rulează pachetele binare pentru server/client. În consolă, va apărea un mesaj care începe cu `Not configured.` Șablonul va fi scris în fișierul indicat de mesaj.

Acel fișier conține toate setările pe care le poți modifica.

## Configurare server
Configurația serverului conține:

 - Port
	 - Portul pe care se conectează clienții. Dacă vei folosi programul prin NAT, este necesară forwardarea acelui port.
 - Interface
	 - Interfața de rețea pe care să asculte după conexiuni. `0.0.0.0` ascultă, în general, pe toate interfețele.
 - Password
	 - Parola pentru criptarea mesajelor.

## Configurare client
Configurația clientului conține:

 - Port
	 - Trebuie să fie exact ca în configurația serverului.
 - Address
	 - Adresa de conectare la server. Pentru o rețea locală, aceasta este adresa locală a serverului. Dacă clientul se conectează prin internet, aceasta trebuie să fie adresa de pe WAN a serverului.
 - Password
	 - Trebuie sa fie exact ca în configurația serverului.
 - Startup
	 - Indică dacă programul va fi pornit automat, când pornește calculatorul
 - DirectoryHidden
	 - Indică dacă directorul unde se află clientul va fi ascuns
 - HideConsole
	 - Indică dacă fereastra consolei va fi ascunsă