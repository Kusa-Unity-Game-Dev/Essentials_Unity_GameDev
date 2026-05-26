# Save System Demo

A minimal, runnable example of the v2 save system. Three files:

| File | What it teaches |
|---|---|
| `CurrencyModule.cs` | The shape of a save module: `[SaveModule]` attribute, `InitializeDefaults()`, and a `Migrate()` step (v1 → v2 adds a dictionary). |
| `PlayerProfileModule.cs` | A second module with nested classes + an enum. Shows `encrypted`/`compressed` attribute flags. |
| `SaveSystemDemo.cs` | The full runtime bootstrap + an on-screen panel to mutate, save, and load. |

## Run it

1. Import this sample (Package Manager → Game Essentials → Samples → Import).
2. Create an empty scene, add an empty GameObject.
3. Add the **SaveSystemDemo** component to it.
4. Press Play.

You'll see a panel: buttons to add coins/gems/levels (in memory), then **Save All** / **Load All**. Mutate, save, stop play, press play again, Load — your values persist.

## What to copy into your own game

The entire bootstrap is `SaveSystemDemo.Start()`:

```csharp
var save = SaveSystem.EnsureInstance();
save.AutoRegisterModules();                       // finds your [SaveModule] classes
if (!await save.Storage.SlotExistsAsync(slot))
    await save.CreateSlotAsync(slot);
await save.LoadAllAsync(slot);
var currency = save.GetModule<CurrencyModule>();  // typed handle for gameplay
// ... later ...
await save.SaveAsync(slot, "currency");
```

Then write your own modules following `CurrencyModule` as the template.

## Try the editor tools against this demo

With the demo running and a slot saved, open:

- **Tools → Save System → Debug Window** — see the `DemoSlot` files.
- **Tools → Save System → Inspector** — open `currency.save`, edit `Coins`, save, then Load in the demo.
- **Tools → Save System → Corruption Lab** — truncate `currency.save`, Trigger Load, watch it recover from backup.
- **Tools → Save System → Fixtures** — capture `DemoSlot` as a fixture, then inject it into a new slot.

See the full guide at `Runtime/saveSystem/SAVE_SYSTEM.md`.
