# MAUI Popups

---

## Features

* Implement any popup design without restrictions
* Customize separate background layer
* Optional anchors for positioning
* Small popup opening time
* Consistent across platforms
* Supports HotReload

## Popup Layers
When rendered your popup is bringing up 2 layers:

* Background dimmer layer, can be totally transparent or dim your background under popup
* Popup content


### Background Layer

At al times you have a background layer filling the entire screen, layout properties will not affect it. You can change it's color by setting BackgroundColor of the popup, for example:

```xml
BackgroundColor="#66000000"
```

would create a semi-transparent dimmer.

You can hide the layer by setting popup `BackgroundColor` property to a totally transparent color.

### Popup Content

Layout properties like `HorizontalOptions`, `VerticalOptions` will affect your popup content.

## The Code

This library was built over CommunityToolkit popups v1, 
the fastest when comparing popup opening time between different libraries.
Consistency across platforms, HotReload support, a separate background layer were added and this library went on a separate jurney.

---

