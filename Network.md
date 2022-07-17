# Retea

O componenta obligatorie pentru plugins este comunicatia intre client si server. Am creat un protocol de retea pe TCP, cu object-oriented design si serializare binara (nu sunt mandru de arhitectura de serializare. Original, vroiam o arhitectura cu generare de cod dinamic, care sa genereze functii de serializare pentru structuri si classuri la runtime, sa parcurga arborele pana ajunge la primitive, dar am vrut sa lucrez si la alte proiecte. Am implementat o serializare unde scriem datele explicit).

Cand scriem plugins, vom interactiona, in general, cu interfata `IBilskirnir`. Acesta are 3 functii pe care le vom folosi:

 - `AddReceiver(IMessageReceiver)`
	 - Adauga un receiver pentru mesaje. Acest lucru este explicat mai jos.

- `RemoveReceiver(IMessageReceiver)`
	- Sterge un receiver.

- `Send(IMessage) -> Task`
	- Trimite un mesaj asincron, si returneaza un task. Este bine sa asteptam completarea acestui task (ne trebuie un context async).

Observatie: toate aceste functii sunt thread safe. Dar nu este indicat sa folosim functia `Send` in paralel, pentru ca mesajele vor ajunge intr-o ordine haotica la client.	

## Receiver

Toata logica de primire (sau chiar si trimitere) se va afla intr-un class care implementeaza interfata `IMessageReceiver`. 

Trebuie implementata o functie pentru a fi folositor: `RegisterHandlers(IHandlerRegister)`. Aici vom adauga listeners pentru pachetele care ne intereseaza. Cand celalalt capat foloseste functia send, mesajele vor ajunge aici. 

`IHandlerRegister` are o functie de interes:
```csharp
Register<T>(Func<T, Task>) unde T : IMessage
```

Aceasta functie primeste un delegat generic catre o functie care primeste instanta mesajului nostru si returneaza un task (putem crea un context async cu acest sistem). Tipul `T` defineste mesajul primit. 

## Message

Toate datele vor fi trimise si primite printr-un message class sau struct. Implementam interfata `IMessage`, care are urmatoarele 2 functii:

- `Serialize(IPacketWriter)`
	- Aici scriem toate datele din instanta pachetului.

- `Deserialize(IPacketReader)`
	- Aici despachetam toate datele din retea in instanta pachetului.

Trebuie sa avem un constructor parameterless, astfel programul nu va putea crea o instanta.

## Serializare

Interactionam cu sistemul de serializare prin interfetele `IPacketWriter` si `IPacketReader`. Acestea au functii pentru scrierea si citirea primitivelor in batch-ul pachetului. Au urmatoarele tipuri de primitive:

- Byte
- Short, UShort
- Int, UInt
- Packed Int, Packed UInt
	- Este un experiment interesant. Compreseaza un numar. Numere sub 127 ajung sa fie 1 byte, si asa mai departe. 
- Long, ULong
- Bytes
	- Aici putem scrie datele noastre. Intern, acesta scrie un Int, lungimea datelor, si apoi datele. La citire, citeste acel int, aloca datele, si apoi le copiaza. Putem sa specificam ca nu vrem sa scrie acel int la inceput, si la citire putem specifica explicit lungimea datelor pe care vrem sa le citim, sau putem citi manual.
- String
- Bool
- Enum
	- Aici putem scrie orice enum vrem, si il citim cu ReadEnum<T>.

## Exemplu

``internal class RemoteTerminal : IMessageReceiver``
Remote Terminal este un class din remote terminal plugin. Aici se trimit si primesc toate comenzile de CMD.

```csharp
public RemoteTerminal(IBilskirnir client)
{
    _client = client;
		...
}
```

Acesta primeste clientul in constructor. De asemenea, am registrat receverul extern acelui class, dar se poate face si din class:
`_client.AddReceiver(_terminal);`

`_client` este instanta clientului, si `_terminal` este instanta RemoteTerminal-ului.

Viata acestui class este controlata de un `RemoteTerminalHandler`. Aceasta creaza/distruge terminale pe baza mesajelor trimise de server.

In `RemoteTerminal` primim comenzi de cmd, si trimitem rezultatele din StdOut.

Aici este implementatia pentru handler (el este un message receiver):

```csharp
public void RegisterHandlers(IHandlerRegister register)
{
    register.Register<SetTerminalStateMessage>(ReceiveSetTerminalState);
    _logger.LogInformation("Registered terminal handler message.");
}
```

`ReceiveSetTerminalState` este o functie care primeste ca parametru `SetTerminalStateMessage` si returneaza un `ValueTask`. 

```csharp
private ValueTask ReceiveSetTerminalState(SetTerminalStateMessage message)
{
    var currentState = _terminal == null 
        ? SetTerminalStateMessage.State.Closed 
        : SetTerminalStateMessage.State.Open;

    if (message.Target == currentState)
    {
        _logger.LogCritical("Invalid state request. Target: {t}, open: {c}", message.Target, _terminal != null);
        return ValueTask.CompletedTask;
    }
    
    if (message.Target == SetTerminalStateMessage.State.Open)
    {
        _terminal = new RemoteTerminal(_client);
        _client.AddReceiver(_terminal);
    }
    else
    {
        _terminal!.Close();
        _client.RemoveReceiver(_terminal);
        _terminal = null;
    }

    return ValueTask.CompletedTask;
}

```

Acel mesaj indica daca terminalul ar trebui deschis sau inchis. Class-ul stocheaza instanta terminalului in "_terminal". Daca este null, inseamna ca terminalul este inchis. Altfel, este deschis. Putem folosi acest fapt sa validam mesajul (daca mesajul doreste sa deschida terminalul, dar el este deja deschis, avem o problema. Daca vrea sa inchida terminalul, dar acesta este inchis, iar avem o problema). 
Observatie: nu am creat un context async pentru ca nu a fost nevoie de unul. Daca as fi trimis mesaje in acea functie, ar fi fost necesar. Asadar, doar returnez un `CompletedTask`.

### Exemplu de trimitere (din RemoteTerminal).
```csharp
private async Task DequeueAndSendAsync()
{
    var sb = new StringBuilder();

    while (!_cts.IsCancellationRequested)
    {
        try
        {
            await Task.Delay(BufferMs, _cts.Token);
            await _cmdOutputQueue.Reader.WaitToReadAsync(_cts.Token);

            while (_cmdOutputQueue.Reader.TryRead(out var line))
            {
                sb.AppendLine(line);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var content = sb.ToString();

        if (!string.IsNullOrEmpty(content))
        {
            await _client.Send(new TextMessage(content));
        }

        sb.Clear();
    }
}
```
Acesta consuma linii de output din StdOut. Asteapta un anumit numar de milisecunde, apoi asteapta sa existe linii de text in buffer. Apoi, le agreghiaza intr-un text mai mare, si apoi trimite textul la server.

Acea agregare (batching) al textului exista deoarece noi citim linii din StdOut, si acestea pot fi numeroase (mii de linii pe secunda, daca executam comenzi de listare de directoare, de exemplu). Fara batching, am trimite mii de mesaje pe secunda (zeci de mii de pachete pe secunda), si ar sufoca reteaua. Acest batching reduce mesajele la o suta pe secunda, ceea ce e mult mai manageable. 