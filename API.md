# Proiectul de plugin

Pentru a folosi API-ul de plugins, descarca codul programului si adauga referinta la proiectul `Yggdrasil.API`

Recomand urmatoarea structura pentru proiectul de plugins:

 - Client
	 -	Aici avem tot codul care ruleaza pe client
- Server
	- Aici avem tot codul care ruleaza pe server
- Messages
	- Aici avem pachetele de retea

# Folosirea API-ului


Pentru a folosi API-ul, va trebui sa creem 2 class-uri de sesiune, una pentru client si una pentru server. Optional, putem crea startup-uri, pentru server si client. Aceste start-upuri permit configurarea containerului de dependente si este necesar cand implementam servicii cu arhitectura mai complicata.

Startup-ul este creat inainte de sesiune, asadar putem injecta orice dependenta din container in sesiune. 

## Startup

Startup-urile sunt identice pentru client si server. Pentru a crea un startup, scriem un class care implementeaza class-ul abstract `ClientPluginStartupBase` pentru client si `ServerPluginStartupBase` pentru server.
Avem 2 functii pe care le putem implementa: `ConfigureHost` si `ConfigureServices`. In general, folosim functia `ConfigureServices`. Acolo, putem adauga dependentele noastre in container.

Un exemplu de implementare:

```csharp
internal sealed class ClientStartup : ClientPluginStartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<BitmapPool>();
    }
}
```

## Comunicatii
Inainte de a continua, recomand citirea documentului [Network](https://github.com/AntonioDumitrescu/WeepingAngel/blob/master/Network.md) pentru a intelege deciziile arhitecturale necesare pentru crearea unui plugin.

## Sesiune

Pentru a crea o sesiune, scriem un class care implementeaza class-ul abstract `ClientPluginBase` pentru client si `ServerPluginBase` pentru server. Putem injecta dependentele din program si din plugin in constructor. Avem 2 functii pe care le putem implementa, anume `StartAsync` si `StopAsync`.

### Client

Sesiunea si startup-ul clientului este distrus/creat in functie de conexiunea la server. Va exista o dependenta de tip `IBilskirnir`, pe care o putem injecta, cu care vom trimite mesaje la server. Putem adauga un handler la DisconnectedEvent-ul clientului pentru a inchide sistemele noastre, fara a folosi `StopAsync` si a face tracking la instante. 

In general, vom avea un Handler class, care primeste clientul, e singleton, si e instantat de sesiune. In functia `StartAsync,` pornim acest handler, daca e cazul (de obicei, registram acel handler ca message receiver in `IBilskirnir` instance). 

In acest handler, vom mentine starea serviciilor noastre (de exemplu, vom primi si procesa mesaje care sa deschida/inchida servicii).

Daca ai implementat acel handler ca `IMessageReceiver`, poti folosi functia `OnClosed` pentru a inchide toate sistemele din acel handler cand se pierde conexiunea. Alternativ, il poti inchide din `StopAsync` al sesiunii.

Exemplu de sesiune:

```csharp
internal sealed class ClientPlugin : ClientPluginBase
{
    private readonly VideoStreamHandler _handler;

    public ClientPlugin(IServiceProvider serviceProvider)
    {
        _handler = ActivatorUtilities.CreateInstance<VideoStreamHandler>(serviceProvider);
    }

    public override Task StartAsync()
    {
        _handler.Start();
        return Task.CompletedTask;
    }
}
```

Handler-ul injecteaza clientul si foloseste acolo reteaua.

```csharp
public VideoStreamHandler(
    ILogger<VideoStreamHandler> logger, 
    IServiceProvider serviceProvider,
    IBilskirnir client,
    BitmapPool bitmapPool)
{
    _logger = logger;
    _serviceProvider = serviceProvider;
    _client = client;
    _bitmapPool = bitmapPool;
}
```

Acesta implementeaza `IMessageReceiver`:

`internal sealed class VideoStreamHandler : IMessageReceiver`

Asadar, putem folosi functia `OnClosed`:

```csharp
public void OnClosed()
{
    _logger.LogInformation("Closing video stream handler.");
    _stream?.Close().RunSynchronously();
}
```

## Server

Sesiunea pentru server este creata cand programul porneste si va persista pe toata durata executiei. *Este obligatoriu sa decoram acest class cu atributul `ServerPlugin`!*

In acel atribut, scriem numele de display al plugin-ului,  pachetul, versiunea, si autorul.

Putem, similar clientului, sa injectam dependente in constructor. Totusi, nu vom avea o instanta de client pe care sa o folosim. Va trebui sa implementam interfata IEventReceiver intr-un class (in general, chiar in sesiune), si sa injectam `IEventManager` in constructor. Apoi, folosim functia `AddReceiver` din event manager pentru a registra sesiunea ca event receiver. Apoi, scriem o functie pentru a injecta interfata noastra in lista de clienti:

```csharp
[SubscribeEvent]
private void OnClientListWindowRender(ClientListWindowRenderEvent @event)
{
    if (@event.SelectedClient == null) return;

    if (ImGui.Button("Open Remote Desktop"))
    {
        _windowManager.HandleClient(@event.SelectedClient);
    }
}

```

Este obligatoriu sa decoram functia cu atributul `SubscribeEvent`, astfel nu vom primi niciun eveniment. Putem injecta dependente in functie. Exemplu:

```csharp
[SubscribeEvent]
private void OnClientListWindowRender(ClientListWindowRenderEvent @event, RemoteDesktopWindowManager manager)
{
    if (@event.SelectedClient == null) return;

    if (ImGui.Button("Open Remote Desktop"))
    {
        manager.HandleClient(@event.SelectedClient);
    }
}
``` 

Cu acel event, putem injecta interfata noastra (un simplu buton). Cand acesta este apasat, adaugam clientul selectat in sistemul nostru.

Exista mai multe tipuri de evenimente, printre care:

 - `ClientListWindowRenderEvent`
	 - Este emis imediat dupa ce s-a randat lista cu clienti
	 - Stocheaza clientul selectat, sau null daca nu exista unul
	 - Stocheaza o lista cu toti clientii

	Folosim acest event pentru a randa interfata in lista cu clienti. O interfata mai complexa ar crea o categorie cu numele plugin-ulu nostru si cu toate optiunile.

- `LoggingMenuRenderEvent`
	- Este emis cand randam categoria de logging din meniu.
	
- `MainMenuRenderEvent`
	- Este emis cand se randeaza bara de meniu. Putem adauga categoriile noastre aici.

- `ProfilingMenuRenderEvent` 
	- Este emis cand randam categoria de profiling din meniu.

- `ViewportRenderEvent`
	-	Este emis cand randam interfata in sine.

- `InputPumpEvent`
	- Este emis cand procesam inputul
	- Contine un snapshot cu inputul catre fereastra


Pe noi ne intereseaza, in principiu, doar `ClientListWindowRenderEvent`. 


# Instalare

Pentru a itera rapid, folsim tool-ul `CopyToServer` pentru a copia dll-ul pentru plugin in folderul `plugins` al server-ului (adaugam o comanda in post-build events. Poti gasi un exemplu in cele 2 plugin-uri incluse). Daca ai folosit biblioteci externe, trebuie sa configurezi build script-ul sa copieze si dll-ul lor intr-un folder numit `libraries` pentru server.
