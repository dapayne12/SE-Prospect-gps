# Introduction

prospect-gps is used to help automatically create GPS coordinates to mark ore
deposits while mining in Space Engineers.

# Setup

Add the string `[PROSPECT-GPS]` to the name of an LCD screen. Load the script
into an programmable block. Turn the programmable block on. You know the script
is working when you see text similar to the following on the LCD screen:

```
Init: 12/30/2024 22:14:50
12/30/2024 23:06:09
```

Both timestamps are UTC. The init line keeps track of when last the script was
loaded into the LCD block. The second timestamp should update approximately
every 30 seconds.

# Using

With the script running. Mine an asteroid. The script keeps track of what type
of ore, and how much ore is mined in each location. Approximately every 30
seconds the LCD will be updated with the latest GPS coordinates to mark the ore
locations:

```
Init: 12/30/2024 22:14:50
12/30/2024 23:06:09

GPS:AG (16.6M) , ICE (2.7M):-3438365:-45789:-2099699:#FF9C00::
GPS:U (2.7M) , CO (57.9M):-3454273:-76915:-2120443:#FF9C00::
```

The GPS coordinates can be copy/pasted into the GPS screen.

# Reset Coordinates

The GPS coordinates on the LCD cannot be manually modified while the script is
running. Any player modifications will be overwritten by the script. To remove
all GPS coordinates from the GPS screen run the script with the `RESET` argument.

# License

MIT License

Copyright (c) 2024 David Payne

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.