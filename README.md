# IRCLib
Barebones .NET IRC library

It'll parse messages into a more manageable format (Source, Command, Tags, Parameters), handle all connection stuff and also has an easy way to add commands via a [Handler] attribute

Works roughly like this:

```csharp
var client = new IRCLib.Client("host[:port]", new IRCLib.User("Username"));

client.RawMessageSent += (sender, args) => Console.WriteLine(">> " + args.Message + "\n");
client.RawMessageReceived += (sender, args) => Console.WriteLine("<< " + args.Message + "\n");

client.Connect();

client.SendRaw("JOIN #channel");
```
