# b7 scripter
A C# scripting interface for [G-Earth](https://github.com/sirjonasxx/G-Earth) which utilizes the
[Xabbo.Common](https://www.github.com/b7c/Xabbo.Common),
[Xabbo.GEarth](https://www.github.com/b7c/Xabbo.GEarth) and
[Xabbo.Core](https://www.github.com/b7c/Xabbo.Core) libraries.

![image](https://user-images.githubusercontent.com/58299468/125163971-d1e1e400-e1e3-11eb-84ca-67560769ea56.png)

## Usage
### Accessing message headers
`Out.Move` / `In.Talk`\
Message names are based on the ones defined in the Unity client.\
For Flash headers to be correctly mapped to the Unity names they must be defined in `messages.ini`.\
You can still access a Flash header by its name: `Out["PlaceObject"]` will return the header for `Out.PlaceRoomItem`.

### Sending, receiving, reading & writing packets
Shout "hello, world":
```cs
Send(Out.Shout, "hello, world", 0);
```

Craft a packet manually and send it:
```cs
var p = new Packet(Out.Shout);
p.WriteString("hello, world");
p.WriteInt(0);
Send(p);
```
\- or -
```cs
var p = new Packet(Out.Shout)
  .WriteString("hello, world")
  .WriteInt(0);
Send(p);
```

Calling `Send` with an incoming header will send the packet to the client:
```cs
Send(In.Chat, -1, "hello, world", 0, 0, 0, 0);
```

Receive and read from a packet:
```cs
Send(Out.GetCredits);
var p = Receive(5000, In.WalletBalance); // wait 5000ms to receive a packet with the WalletBalance header
p.ReadString() // Returns the amount of credits in your wallet
```
If the final statement in a script excludes the semicolon `;` it will become the return value and be output to the log.

Packet deconstruction:
```cs
var p = CaptureOut(-1, Out.Move); // capture an outgoing move packet, use -1 for no timeout
var (x, y) = p.Read<int, int>();
Log($"Moving to {x}, {y}.");
```

### Intercepting and blocking packets, modifying data

To register an intercept callback for a certain header:
```cs
OnIntercept(Out.Move, e => Log(e.Packet.Read<int, int>()));
```
Now each time you click a tile to move, the packet will be intercepted and the tile coordinates will be output to the log.

**Note: All callbacks registered within a script are deregistered upon the script's completion,\
so you must call `Wait` to pause execution and keep the script alive until it is cancelled.**

To block packets, call `Block` on the `InterceptArgs` passed into the callback:
```cs
OnIntercept(Out.Move, e => e.Block());
```

Modifying values in a packet:
```cs
// skip an int, string, int, then replace an int with 38
OnIntercept(In.Chat, e => e.Packet.Replace(Int, Str, Int, 38));
```
```cs
// replace a string from the 5th byte (0 based index -> 4, skips the first 4-byte integer)
// using a transform function to change it to uppercase
OnIntercept(In.Chat, e => e.Packet.ReplaceAt(4, s => s.ToUpper()));
```

### Interactions
There are various methods defined in the scripter globals class (`G`) to make it easier to interact with the game.\
These are just a few of the methods available.

Talk, shout or whisper:
```cs
Talk("Hello, world");
Shout("Hello, world!");
Whisper("world", "Hello, world.");
```

Move to a tile:
```cs
Move(5, 6);
```

### Game state
Game state is being managed in the background to provide information about the current state of the room, its furni and entities, etc.\
The user, bot, pet, furni count; room, profile and connection status can be seen in the toolbar at the bottom right of the application.

Get the current room ID:
```cs
RoomId
```

List all users in the room:
```cs
foreach (var user in Users)
  Log(user.Name);
```

Count furni in the room:
```cs
Furni.Count()
FloorItems.Count()
WallItems.Count()
```

### Game data support

The current furni, figure, product data and external texts are loaded when the scripter starts.\
They can be accessed using `FurniData`, `FigureData`, `ProductData` and `Texts` respectively.\
To get the data of a furni you can use `FurniData.GetInfo(ItemType type, int kind)` where `type` is either `ItemType.Floor` or `ItemType.Wall`, and `kind` is the furni's ID specifier. (ex. `ItemType.Floor, 179` = Rubber Duck).

Each furni also has a unique string identifier (its "class name").\
For example the rubber duck's identifier is `duck`, and this can be accessed using `FurniData["duck"]`.

There are also extension methods for easily retrieving the furni info, name, etc. from an `IItem` instance.\
As all item classes (floor/wall furni, inventory, trade, catalog, marketplace items) implement the `IItem` interface, these extension methods can be called on any one of them.\
For example, to grab the first furni in a room and display its info:
```cs
var furni = Furni.First();
string name = furni.GetName();
Log($"Item name: {name}");
var info = furni.GetInfo();
Log(ToJson(furni.GetInfo()));
```

Enumerables of `IItem` can be filtered by furni info:
```cs
// find furni info by name
// note this may not be the exact info you're looking for
// if another furni shares the same name
var info = FurniData.FindFloorItem("Rubber Duck");
int count = Furni.OfKind(info).Count();
Log($"There are {count} {info.Name}'s in the room");
```

Or a furni identifier:
```cs
Furni.OfKind("duck").Count() // count the number of Rubber Duck furni in the room
```

This can be useful for example to pick up all furni in the room of a specific type:
```cs
// pick up all Rubber Duck furni in the room
foreach (var item in Furni.OfKind("duck")) {
  Pickup(item);
  Delay(150);
}
```

## Example scripts
- Output the current room's floor plan to a text file:
```cs
File.WriteAllText($"floorplan-{Room.Id}.txt", FloorPlan.OriginalString);
```

- Send a friend request to everyone in the room:
```cs
foreach (var user in Users) {
  if (user == Self) continue; // skip self
  Send(Out.RequestFriend, user.Name);
  // display a whisper from the user for visual feedback
  ShowBubble("friend request sent", index: user.Index);
  Delay(1000);
}
```

List the name and count of all furni in the room:
```cs
// group all furni by its descriptor using the IItem.GetDescriptor() extension method
// the descriptor includes the item type, kind and a variant string (used for posters).
foreach (var group in Furni.GroupBy(furni => furni.GetDescriptor())) {
  string name = group.Key.GetName();
  int count = group.Count();
  Log($"{count,6:N0} x {name}");
}
```
This can also be done with your inventory. Just replace Furni with `GetInventory()`.\
Note you must be in a room to load your inventory.
```cs
var inventory = GetInventory();
foreach (var group in inventory.GroupBy(item => item.GetDescriptor()))
  Log($"{group.Count(),6:N0} x {group.Key.GetName()}");
```

- Download all photos in a room to the directory `photos/roomId`:
```cs
string dir = $"photos/{Room.Id}";
Directory.CreateDirectory(dir);
var photos = WallItems.OfKind("external_image_wallitem_poster_small").ToArray();
for (int i = 0; i < photos.Length; i++) {
  string filePath = Path.Combine($"photos/{Room.Id}", $"{photos[i].Id}.png");
  if (File.Exists(filePath)) continue;
  Log($"Downloading {i+1}/{photos.Length}");
  try {
    var photoInfo = System.Text.Json.JsonSerializer.Deserialize<PhotoInfo>(photos[i].Data);
    var photoData = await H.GetPhotoDataAsync(photoInfo.Id);
    byte[] image = await H.DownloadPhotoAsync(photoData);
    File.WriteAllBytes(filePath, image);
  } catch (Exception ex) {
    Log($"Failed to download: {ex.Message}");
  }
}
```
