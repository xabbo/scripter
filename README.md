# xabbo scripter
A C# scripting interface for [G-Earth](https://github.com/sirjonasxx/G-Earth).\
Powered by the
[Xabbo.Common](https://www.github.com/b7c/Xabbo.Common),
[Xabbo.GEarth](https://www.github.com/b7c/Xabbo.GEarth) and
[Xabbo.Core](https://www.github.com/b7c/Xabbo.Core) libraries.

See [this repository](https://www.github.com/b7c/xabbo-scripts) for a collection of useful scripts and examples of what is possible with the scripter.

![image](https://user-images.githubusercontent.com/58299468/181401866-6950ee4b-6bcc-49bb-a35b-24f2798b33d0.png)

## Usage

### Naming & grouping scripts
To name scripts and sort them into groups within the script list,\
use the following syntax at the top of the file:
```cs
/// @name Script name
/// @group Group name
```

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
var p = Receive(In.WalletBalance, 5000); // wait 5000ms to receive a packet with the WalletBalance header
p.ReadString() // returns the amount of credits in your wallet
```
If the final statement in a script excludes the semicolon `;` it will become the return value and be output to the log.

Packet deconstruction:
```cs
var p = Receive(Out.Move); // capture an outgoing move packet, omit timout or use -1 for no timeout
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
There are various methods defined in the scripter globals class ([source](https://github.com/b7c/Xabbo.Scripter/blob/master/Xabbo.Scripter.Common/Scripting/G.cs)) to make it easier to interact with the game.\
These are just a few of the methods available.

Talk, shout or whisper:
```cs
Talk("Hello, world");
Shout("Hello, world!");
Whisper("recipient", "Hello, world.");
```

Move to a tile:
```cs
Move(5, 6);
```

Search for "shop" in the navigator and display results where there is at least 1 user in the room:
```cs
foreach (var room in QueryNav("shop")
    .Where(x => x.Users > 0)
    .OrderByDescending(x => x.Users)) {
  Log(
    $"\"{room.Name}\" by {room.OwnerName}"
    + $" ({room.Users}/{room.MaxUsers} users)"
  );
}
```

Retrieve and log the content of all sticky notes in the room:
```cs
foreach (var item in WallItems.OfCategory(FurniCategory.Sticky)) {
  var sticky = GetSticky(item);
  Log($"\"{sticky.Text}\"\n");
  Delay(1000);
}
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

List the name and count of all furniture in the room:
```cs
// Group furni by its descriptor
// which includes it type (floor/wall), kind (furni id)
// and variant (for posters, eg. "9" = Rainforest Poster)
var groups = Furni
  .GroupBy(furni => furni.GetDescriptor())
  .OrderByDescending(group => group.Count());
// Display the count and name of each furni group
foreach (var (descriptor, items) in groups)
  Log($"{items.Count(),6:N0} x {descriptor.GetName()}");
```

### Game data support

The current furni, figure, product data and external texts are loaded when the scripter starts.\
They can be accessed using `FurniData`, `FigureData`, `ProductData` and `Texts` respectively.\
To get the info of a furni you can use `FurniData.GetInfo(ItemType type, int kind)` where `type` is either `ItemType.Floor` or `ItemType.Wall`, and `kind` is the furni's ID specifier. (ex. `ItemType.Floor, 179` = Rubber Duck).

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

return info;
```

Enumerables of `IItem` can be filtered by a furni info:
```cs
// Find furni info by name
// Note this may not be the exact info you're looking for
// if another furni shares the same name
var info = FurniData.FindFloorItem("Rubber Duck");
int count = Furni.OfKind(info).Count();
Log($"There are {count} {info.Name}s in the room");
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

### Events

Callbacks for specific events can be registered using methods that begin with `On...`.\
The packet data is parsed into a more easily consumable `EventArgs` instance which is passed into the callback, so it is not necessary to read the packet structure yourself.

For example, to walk to a chair when it gets placed:
```cs
OnFloorItemAdded(e => {
  if (e.Item.GetInfo().CanSitOn)
    Move(e.Item.Location);
});
```

## Example scripts
Output the current room's floor plan to a text file:
```cs
File.WriteAllText($"floorplan-{Room.Id}.txt", FloorPlan.OriginalString);
```

Send a friend request to everyone in the room:
```cs
foreach (var user in Users) {
  if (user == Self) continue; // skip self
  Send(Out.RequestFriend, user.Name);
  // display a whisper from the user for visual feedback
  ShowBubble("*friend request sent*", index: user.Index);
  Delay(1000);
}
```

Download all photos in a room to the directory `photos/roomId`:
```cs
string dir = $"photos/{Room.Id}";
Directory.CreateDirectory(dir);
var photos = WallItems.OfKind("external_image_wallitem_poster_small").ToArray();
for (int i = 0; i < photos.Length; i++) {
  Ct.ThrowIfCancellationRequested();
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
