// Usage:
// Build an LCD with [PROSPECT-GPS] in its name. Recompile the script after
// building the panel to ensure it's detected. As you mine asteroids GPS
// coordinates will be added to the panel. The coordinate names will contain
// resource type and sizes.
//
// IMPORTANT! If you re-mine an astroid that already has an entry in the GPS
// then the new ore amounts will be added to the existing ore amounts. This is
// probably undesirable.
//
// Run the script with the argument "RESET" to remove all GPS coordinates from
// memory and the LCD. Removing coordinates from LCD only is not enough, the
// coordinates will still exist in the scirpt memory.

// Output will be saved to the first LCD screen found that contains
// this string in its name.
static readonly string OUTPUT_GPS_TOKEN = "[PROSPECT-GPS]";

// Ore mined within this many meters will be condensed into a single
// GPS coordinate.
static readonly long COORDINATE_RANGE_METERS = 3000;

// Aproximate number of seconhds between each update.
static readonly int SECONDS_BETWEEN_UPDATE = 30;

//////////////////////////////////////////////////////////////
/// End of configuration. No modifications beyond this point.
//////////////////////////////////////////////////////////////

static readonly MyItemType PT = new MyItemType("MyObjectBuilder_Ore", "Platinum");
static readonly MyItemType U = new MyItemType("MyObjectBuilder_Ore", "Uranium");
static readonly MyItemType AU = new MyItemType("MyObjectBuilder_Ore", "Gold");
static readonly MyItemType AG = new MyItemType("MyObjectBuilder_Ore", "Silver");
static readonly MyItemType MG = new MyItemType("MyObjectBuilder_Ore", "Magnesium");
static readonly MyItemType CO = new MyItemType("MyObjectBuilder_Ore", "Cobalt");
static readonly MyItemType SI = new MyItemType("MyObjectBuilder_Ore", "Silicon");
static readonly MyItemType NI = new MyItemType("MyObjectBuilder_Ore", "Nickel");
static readonly MyItemType FE = new MyItemType("MyObjectBuilder_Ore", "Iron");
static readonly MyItemType ICE = new MyItemType("MyObjectBuilder_Ore", "Ice");

// The ores, in priority order.
static readonly Dictionary<MyItemType, string> ORE_SYMBOLS = new Dictionary<MyItemType, string>{
    { PT, "PT" },
    { U, "U" },
    { AU, "AU" },
    { AG, "AG" },
    { MG, "MG" },
    { CO, "CO" },
    { SI, "SI" },
    { NI, "NI" },
    { FE, "FE" },
    { ICE, "ICE" }
};

class FVector {
    public long X;
    public long Y;
    public long Z;

    private FVector() {}

    public FVector(Vector3D vector) {
        X = (long)vector.X;
        Y = (long)vector.Y;
        Z = (long)vector.Z;
    }

    public FVector(long x, long y, long z) {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

    public long Distance(FVector vector) {
        return (long)Math.Sqrt(
            Math.Pow(this.X - vector.X, 2) +
            Math.Pow(this.Y - vector.Y, 2) +
            Math.Pow(this.Z - vector.Z, 2));
    }
}

class GPSCoordinate {
    public FVector location;
    public Dictionary<MyItemType, long> ores = new Dictionary<MyItemType, long>();

    public GPSCoordinate(FVector location) {
        this.location = location;
    }

    public void AddOres(Dictionary<MyItemType, long> ores) {
        foreach (MyItemType type in ores.Keys) {
            if (this.ores.ContainsKey(type)) {
                this.ores[type] += ores[type];
            } else {
                this.ores[type] = ores[type];
            }
        }
    }

    override public string ToString() {
        string coordinate = "GPS:";
        bool firstOre = true;
        foreach (MyItemType type in ORE_SYMBOLS.Keys) {
            if (!ores.ContainsKey(type)) {
                continue;
            }

            if (firstOre) {
                firstOre = false;
            } else {
                coordinate += " , ";
            }
            coordinate += $"{ORE_SYMBOLS[type]} ({FormatAmount(ores[type])})";
        }
        coordinate += $":{location.X}:{location.Y}:{location.Z}:#FF75C9F1::";

        return coordinate;
    }

    private string FormatAmount(long amount) {
        if (amount < 1000) {
            return amount.ToString();
        }

        if (amount < 1000000) {
            return $"{Math.Round(amount / (double)1000, 1)}K";
        }

        if (amount < 1000000000) {
            return $"{Math.Round(amount / (double)1000000, 1)}M";
        }

        if (amount < 1000000000000) {
            return $"{Math.Round(amount / (double)1000000000, 1)}B";
        }

        if (amount < 1000000000000000) {
            return $"{Math.Round(amount / (double)1000000000000, 1)}T";
        }

        throw new Exception($"Amount too large: {amount}");
    }
}

IMyTextPanel outputPanel = null;
List<IMyInventory> inventories = new List<IMyInventory>();
List<GPSCoordinate> coordinates = new List<GPSCoordinate>();

DateTime initTime = DateTime.UtcNow;
public Program() {
    initTime = DateTime.UtcNow;

    FindOutputLCD();

    List<IMyEntity> entitiesWithInventory = new List<IMyEntity>();
    GridTerminalSystem.GetBlocksOfType(entitiesWithInventory, entity => entity.HasInventory);
    foreach (IMyEntity entity in entitiesWithInventory) {
        inventories.Add(entity.GetInventory());
    }

    lastOreCount = GetOreCount();
    OutputCoordinates();

    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

DateTime nextRunTime = DateTime.UtcNow;
public void Main(string argument) {
    if (argument == "RESET") {
        if (outputPanel != null) {
            string message = $"RESET command: {DateTime.UtcNow}\n" +
                $"Init time: {initTime}\n" +
                $"{ReadTextFromLCD(outputPanel)}\n\n";
            outputPanel.CustomData += message;
        }

        lastOreCount = GetOreCount();
        coordinates = new List<GPSCoordinate>();
        OutputCoordinates();
        return;
    }

    DateTime now = DateTime.UtcNow;
    if (now >= nextRunTime) {
        nextRunTime = now.AddSeconds(SECONDS_BETWEEN_UPDATE);
    } else {
        return;
    }

    if (outputPanel == null) {
        bool found = FindOutputLCD();
        if (!found) {
            return;
        }
    }

    Dictionary<MyItemType, long> oreCount = GetOreCount();
    Dictionary<MyItemType, long> newOre = GetNewOre(oreCount);

    if (newOre.Keys.Count == 0) {
        OutputCoordinates();
        return;
    }

    GPSCoordinate coordinate = GetGPSCoordinate();
    coordinate.AddOres(newOre);

    OutputCoordinates();
}

private Dictionary<MyItemType, long> GetOreCount() {
    Dictionary<MyItemType, long> oreCount = new Dictionary<MyItemType, long>();

    foreach (IMyInventory inventory in inventories) {
        List<MyInventoryItem> items = new List<MyInventoryItem>();
        inventory.GetItems(items, item => ORE_SYMBOLS.ContainsKey(item.Type));
        foreach (MyInventoryItem item in items) {
            if (oreCount.ContainsKey(item.Type)) {
                oreCount[item.Type] += (long)item.Amount;
            } else {
                oreCount[item.Type] = (long)item.Amount;
            }
        }
    }

    return oreCount;
}

Dictionary<MyItemType, long> lastOreCount = null;
private Dictionary<MyItemType, long> GetNewOre(Dictionary<MyItemType, long> oreCount) {
    if (lastOreCount == null) {
        lastOreCount = oreCount;
        return oreCount;
    }

    Dictionary<MyItemType, long> newOre = new Dictionary<MyItemType, long>();

    foreach (MyItemType item in oreCount.Keys) {
        if (lastOreCount.ContainsKey(item)) {
            long difference = oreCount[item] - lastOreCount[item];
            if (difference > 0) {
                newOre[item] = difference;
            }
        } else {
            newOre[item] = oreCount[item];
        }
    }

    lastOreCount = oreCount;
    return newOre;
}

private GPSCoordinate GetGPSCoordinate() {
    FVector position = new FVector(Me.GetPosition());

    foreach (GPSCoordinate coordinate in coordinates) {
        if (position.Distance(coordinate.location) < COORDINATE_RANGE_METERS) {
            return coordinate;
        }
    }

    GPSCoordinate newCoordinate = new GPSCoordinate(position);
    coordinates.Add(newCoordinate);
    return newCoordinate;
}

private void OutputCoordinates() {
    if (outputPanel == null) {
        return;
    }

    string output = $"Init: {initTime}\n";
    output += $"{DateTime.UtcNow}\n\n";
    foreach (GPSCoordinate coordinate in coordinates) {
        output += $"{coordinate}\n";
    }
    outputPanel.WriteText(output);
}

private GPSCoordinate ParseGPSLine(string gpsLine) {
    try {
        string[] lineTokens = gpsLine.Split(':');
        string gpsName = lineTokens[1];
        long x = long.Parse(lineTokens[2]);
        long y = long.Parse(lineTokens[3]);
        long z = long.Parse(lineTokens[4]);

        FVector vector = new FVector(x, y, z);
        GPSCoordinate coordinate = new GPSCoordinate(vector);

        Dictionary<MyItemType, long> ores = new Dictionary<MyItemType, long>();
        string[] oresString = gpsName.Split(',');
        foreach (string oreString in oresString) {
            string[] oreTokens = oreString.Split(' ');

            string oreSymbol = oreTokens[0].Trim();
            MyItemType ore = GetOreTypeFromSymbol(oreSymbol);

            string amountString = oreTokens[1].Trim(new char[]{'\n', ' ', '(', ')'});
            long amount = 0;
            if (amountString.EndsWith("K")) {
                amountString = amountString.Substring(0, amountString.Length - 1);
                amount = (long)(double.Parse(amountString) * 1000);
            } else if (amountString.EndsWith("M")) {
                amountString = amountString.Substring(0, amountString.Length - 1);
                amount = (long)(double.Parse(amountString) * 1000000);
            } else if (amountString.EndsWith("B")) {
                amountString = amountString.Substring(0, amountString.Length - 1);
                amount = (long)(double.Parse(amountString) * 1000000000);
            } else if (amountString.EndsWith("T")) {
                amountString = amountString.Substring(0, amountString.Length - 1);
                amount = (long)(double.Parse(amountString) * 1000000000000);
            } else {
                amount = long.Parse(amountString);
            }

            ores[ore] = amount;
        }

        coordinate.AddOres(ores);
        return coordinate;
    } catch {
        return null;
    }
}

private MyItemType GetOreTypeFromSymbol(string symbol) {
    foreach (MyItemType key in ORE_SYMBOLS.Keys) {
        if (ORE_SYMBOLS[key] == symbol) {
            return key;
        }
    }

    throw new Exception($"No ore type found for symbol: {symbol}");
}

private bool FindOutputLCD() {
    List<IMyTextPanel> panels = new List<IMyTextPanel>();
    GridTerminalSystem.GetBlocksOfType(panels, block => block.IsSameConstructAs(Me));
    bool found = false;
    foreach (IMyTextPanel panel in panels) {
        if (panel.CustomName.Contains(OUTPUT_GPS_TOKEN)) {
            outputPanel = panel;
            outputPanel.ContentType = ContentType.TEXT_AND_IMAGE;
            found = true;
            break;
        }
    }

    if (found) {
        string panelText = ReadTextFromLCD(outputPanel);
        if (outputPanel != null) {
            string message = $"FindOutputLCD: {DateTime.UtcNow}\n" +
                $"Init time: {initTime}\n" +
                $"{panelText}\n\n";
            outputPanel.CustomData += message;
        }

        coordinates = new List<GPSCoordinate>();
        foreach (string line in panelText.Split('\n')) {
            string trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("GPS:")) {
                GPSCoordinate coordinate = ParseGPSLine(trimmedLine);
                if (coordinate != null) {
                    coordinates.Add(coordinate);
                }
            }
        }
    }

    return found;
}

private string ReadTextFromLCD(IMyTextPanel textPanel) {
    StringBuilder builder = new StringBuilder();
    textPanel.ReadText(builder);
    return builder.ToString();
}