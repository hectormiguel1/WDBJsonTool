# WDBJsonTool Architecture

## Overview

WDBJsonTool is a bidirectional conversion tool for WDB (White Data Binary) files used in Final Fantasy XIII series games. It converts between proprietary binary database files and human-readable JSON format.

## Supported Formats

- **XIII**: Final Fantasy XIII WDB format
- **XIII2LR**: Final Fantasy XIII-2 and Lightning Returns WDB format (enhanced with string arrays)

## High-Level Architecture

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐
│  WDB Binary │ ──────> │  Extraction  │ ──────> │    JSON     │
│    File     │ <────── │  Conversion  │ <────── │    File     │
└─────────────┘         └──────────────┘         └─────────────┘
```

## Pipeline Architecture

### Extraction Pipeline (WDB → JSON)

```
WDB File
   │
   ├─> 1. Validate File Header (Magic bytes: "WPD")
   │
   ├─> 2. Read Record Count
   │
   ├─> 3. Parse Metadata Sections
   │      │
   │      ├─> !!version (format version)
   │      ├─> !!typelist (field type list)
   │      ├─> !!strtypelist (field data type indicators)
   │      ├─> !!string (string pool)
   │      ├─> !!sheetname (table name) [XIII2LR only]
   │      ├─> !structitem (field definitions) [XIII2LR only]
   │      └─> !!strArray* (string array sections) [XIII2LR only]
   │
   ├─> 4. Parse Record Data
   │      │
   │      ├─> For each record:
   │      │   ├─> Read record name (16 bytes)
   │      │   ├─> Read record data (16 bytes)
   │      │   └─> Unpack fields based on strtypelist
   │      │
   │      └─> Field unpacking:
   │          ├─> Bitpacked fields → extract variable-width values
   │          ├─> Float fields → read 32-bit float
   │          ├─> String fields → read offset, lookup in !!string
   │          ├─> UInt fields → read 32-bit or 64-bit uint
   │          └─> StrArray fields → read index, lookup in !!strArray [XIII2LR]
   │
   └─> 5. Write JSON
          ├─> Metadata sections as arrays/values
          └─> Records as array of objects
```

### Conversion Pipeline (JSON → WDB)

```
JSON File
   │
   ├─> 1. Parse JSON
   │
   ├─> 2. Deserialize Metadata
   │      ├─> Record count
   │      ├─> Version
   │      ├─> Type lists
   │      ├─> Field definitions
   │      └─> String array info [XIII2LR only]
   │
   ├─> 3. Deserialize Records
   │      ├─> Read each record object
   │      ├─> Extract field values
   │      └─> Store in memory structures
   │
   ├─> 4. Convert Records to Binary
   │      │
   │      ├─> Build string pool (!!string section)
   │      ├─> Build string arrays (!!strArray* sections) [XIII2LR only]
   │      │
   │      └─> For each record:
   │          ├─> Bitpacked fields → pack into 32-bit chunks
   │          ├─> Float fields → convert to binary
   │          ├─> String fields → get offset from string pool
   │          ├─> UInt fields → convert to binary
   │          └─> StrArray fields → pack index into bitfield [XIII2LR]
   │
   └─> 5. Build WDB File
          ├─> Write header (magic bytes, record count)
          ├─> Write metadata sections
          └─> Write record data
```

## Component Breakdown

### 1. Entry Points

#### XIII/Extraction/ExtractionMain.cs
- Orchestrates WDB → JSON extraction for XIII format
- Handles dual-mode parsing:
  - **Known files**: Uses predefined field names from WDBDicts
  - **Unknown files**: Generates generic field names

#### XIII2LR/Extraction/ExtractionMain.cs
- Orchestrates WDB → JSON extraction for XIII2LR format
- Always treats files as "known" (reads metadata from file)
- Calls StrArrayParser for string array processing

#### XIII/Conversion/ConversionMain.cs
- Orchestrates JSON → WDB conversion for XIII format
- Branches between known/unknown conversion modes

#### XIII2LR/Conversion/ConversionMain.cs
- Orchestrates JSON → WDB conversion for XIII2LR format
- Handles string array reconstruction

### 2. Parsing Components

#### SectionsParser
- **Purpose**: Extracts metadata sections from WDB binary
- **Sections handled**:
  - `!!version`: Format version number
  - `!!typelist`: Type information
  - `!!strtypelist`: Field data type indicators (0=bitpacked, 1=float, 2=string, 3=uint)
  - `!!string`: Null-terminated string pool
  - `!!sheetname`: Table name [XIII2LR only]
  - `!structitem`: Field definitions [XIII2LR only]
- **Key logic**: Section detection by name, data extraction to byte arrays

#### RecordsParser
- **Purpose**: Extracts field data from record binary chunks
- **Field type handling**:
  - **Bitpacked (type 0)**: Variable-width values packed MSB-first in 32-bit chunks
  - **Float (type 1)**: Standard IEEE 754 float
  - **String offset (type 2)**: Index into !!string section
  - **UInt (type 3)**: 32-bit or 64-bit unsigned integer
- **XIII-specific**: Dual methods (ParseRecordsWithFields / ParseRecordsWithoutFields)
- **XIII2LR-specific**: Single ProcessRecords method, handles "s#" string array fields

#### StrArrayParser [XIII2LR only]
- **Purpose**: Handles complex string array sections
- **Sections processed**:
  - `!!strArray`: Packed string data
  - `!!strArrayInfo`: Metadata (offsets per value, bits per offset)
  - `!!strArrayList`: List of string array names
- **Key challenge**: Bitpacked offset indirection for space efficiency
- **Algorithm**: Unpacks bitpacked offsets → resolves string positions → builds dictionaries

### 3. Conversion Components

#### JsonDeserializer
- **Purpose**: Reads JSON and populates variable objects
- **Two-phase deserialization**:
  1. Main sections (metadata)
  2. Records (data)
- **Validation**: Token type checking, property name verification
- **Output**: Populated WDBVariables object with typed data

#### RecordsConversion
- **Purpose**: Converts record data from C# objects to binary format
- **Responsibilities**:
  - String pool construction (deduplication)
  - String array packing [XIII2LR only]
  - Bitpacking field values
  - Binary data generation per record
- **XIII-specific**: ConvertRecordsWithFields / ConvertRecordsNoFields
- **XIII2LR-specific**: ConvertRecordsStrArray / ConvertRecords

#### WDBBuilder
- **Purpose**: Assembles final binary WDB file
- **Build order**:
  1. File header (magic bytes, record count)
  2. Record name + data pairs
  3. !!string section
  4. !!strArray* sections [XIII2LR only]
  5. !!strtypelist section
  6. !!typelist section
  7. !!version section
- **Section structure**: 16-byte header (name + length) + data

### 4. Support Components

#### WDBVariables Classes
- **Purpose**: Stateful containers for parsed/constructed data
- **Shared properties**: Record count, field count, section data, dictionaries
- **XIII-specific**: IsKnown flag, WDBDicts lookup
- **XIII2LR-specific**: String array dictionaries, offset packing metadata

#### SharedMethods
- **Purpose**: Common utility functions
- **Key methods**:
  - `DeriveUIntFromSectionData`: Extract uint from byte array
  - `DeriveFloatFromSectionData`: Extract float from byte array
  - `DeriveStringFromArray`: Extract null-terminated string
  - `SaveSectionData`: Read section data into byte array
  - `CreateArrayFromUIntList`: Convert uint list to byte array
  - `DeriveFieldNumber`: Parse bit count from field name

#### BitOperationHelpers
- **Purpose**: Binary/bitwise operation utilities
- **Key operations**:
  - `UIntToBinary`: Convert uint to binary string
  - `BinaryToInt`: Convert binary string to signed int
  - `BinaryToUInt`: Convert binary string to unsigned int
  - Bitpacking/unpacking logic

#### Extensions
- **BinaryReader extensions**: ReadBytesString, ReadBytesUInt32, etc.
- **Purpose**: Simplify common binary reading patterns

#### JsonMethods
- **Purpose**: JSON validation and token utilities
- **Key methods**:
  - `CheckTokenType`: Validate expected token type
  - `CheckPropertyName`: Validate property name
  - `GetNumbersFromArrayPropertyUInt`: Extract uint array
  - `GetStringsFromArrayProperty`: Extract string array

## Field Type System

### Field Name Encoding

Field names encode the data type and bit width:
```
Pattern: [prefix][bitcount]
Examples:
  - i8     → 8-bit signed integer
  - u16    → 16-bit unsigned integer
  - f32    → 32-bit float (bitpacked)
  - s4     → 4-bit string array index [XIII2LR only]
  - i0/i32 → Full 32-bit signed integer
```

### Prefixes
- `i` - Signed integer
- `u` - Unsigned integer
- `f` - Float (stored as bitpacked int)
- `s` - String array reference [XIII2LR only]

### Bitpacking Algorithm

**Concept**: Multiple small fields are packed into 32-bit chunks to save space.

**Example**:
```
Fields: u4, i8, u12, u8  (total: 32 bits)
Binary chunk: [u4][i8][u12][u8] → packed into single uint32

Unpacking order: MSB → LSB (Most Significant Bit first)
Read position: Start at bit 31, work down to bit 0
```

**Special cases**:
- Field with bit count 0 or 32 → uses entire 32-bit chunk
- Field larger than remaining bits → deferred to next chunk

## Key Architectural Differences: XIII vs XIII2LR

| Aspect | XIII | XIII2LR |
|--------|------|---------|
| **Field Source** | Hardcoded in WDBDicts.cs | Dynamic from !structitem section |
| **Known Files** | Explicit IsKnown flag | All files are "known" |
| **String Arrays** | Not supported | !!strArray* sections |
| **Sheet Name** | Looked up from dictionary | Stored in !!sheetname section |
| **Parsing Modes** | Dual (WithFields/WithoutFields) | Single (always has fields) |
| **Record Parsing** | Two separate methods | One unified method |
| **String Array Fields** | N/A | "s#" prefix for bitpacked indices |

## Data Flow Example

### Extraction Example (XIII2LR)

```
Input WDB:
  Header: WPD, RecordCount=100
  Section !!version: 0x00000001
  Section !!strtypelist: [0, 1, 2, 3, 0, ...]
  Section !structitem: ["i8health", "f32speed", "name", "u32id", "s4type"]
  Section !!strArray: "warrior\0mage\0thief\0..."
  Record "player001": [binary data]

Processing:
  1. ExtractionMain validates header
  2. SectionsParser extracts:
     - version → 1
     - strtypelist → [0, 1, 2, 3, 0]
     - structitem → field names array
  3. StrArrayParser unpacks !!strArray
     - Creates dictionary: "s4type" → ["warrior", "mage", "thief", ...]
  4. RecordsParser processes "player001":
     - Type 0 (bitpacked): Unpack i8health
     - Type 1 (float): Read f32speed
     - Type 2 (string): Read offset, lookup "name" in !!string
     - Type 3 (uint): Read u32id
     - Type 0 (bitpacked): Unpack s4type index, lookup in strArray dict

Output JSON:
{
  "recordCount": 100,
  "!!version": 1,
  "!!strtypelist": [0, 1, 2, 3, 0],
  "!structitem": ["i8health", "f32speed", "name", "u32id", "s4type"],
  "!!records": [
    {
      "record": "player001",
      "i8health": 75,
      "f32speed": 5.5,
      "name": "Hero",
      "u32id": 1001,
      "s4type": "warrior"
    },
    ...
  ]
}
```

## Extension Points

### Adding a New WDB Format

1. Create new namespace (e.g., `XIII3`)
2. Implement WDBVariables class
3. Implement ExtractionMain
4. Implement SectionsParser (handle format-specific sections)
5. Implement RecordsParser (handle format-specific field types)
6. Implement ConversionMain
7. Implement RecordsConversion
8. Implement WDBBuilder
9. Update Program.cs to recognize new format

### Adding a New Field Type

1. Add constant to FieldTypeConstants
2. Add prefix to FieldPrefixConstants (if bitpacked)
3. Update RecordsParser switch statement (extraction)
4. Update RecordsConversion switch statement (conversion)
5. Update JsonDeserializer if needed (deserialization)

### Adding a New Section Type

1. Add section name constant to WDBVariables
2. Add section name length constant
3. Update SectionsParser to recognize section
4. Add parsing logic to extract section data
5. Update WDBBuilder to write section
6. Update JsonDeserializer if section appears in JSON

## Testing Strategy

### Unit Testing
- BitOperationHelpers: Binary conversion correctness
- SharedMethods: Data extraction from byte arrays
- FieldTypeConstants: Constant values

### Integration Testing
- Round-trip testing: WDB → JSON → WDB → verify binary match
- Field type testing: Verify each type extracts/converts correctly
- Section testing: Verify each section parses/builds correctly

### Validation Testing
- Malformed WDB files: Header validation, section validation
- Invalid JSON: Type checking, property validation
- Edge cases: Empty strings, zero-bit fields, maximum values

## Performance Considerations

### Memory Usage
- Large WDB files load entirely into memory
- String pools are deduplicated during conversion
- Record dictionaries scale with record count

### Optimization Opportunities
- Stream-based processing for large files
- Lazy loading of sections
- Parallel record processing
- String pool pre-allocation

## Known Limitations

1. **Memory-bound**: Entire files loaded into memory
2. **No incremental processing**: Must process complete file
3. **Limited error recovery**: Errors typically abort processing
4. **No format validation**: Assumes well-formed input
5. **XIII WDBDicts**: Hardcoded database requires updates for new files

## Future Enhancements

1. **Streaming API**: Process files without full load
2. **Schema validation**: JSON schema for format validation
3. **Interactive mode**: Field-by-field extraction/editing
4. **Diff/merge tools**: Compare WDB files, merge changes
5. **GUI application**: Visual editing of WDB data
6. **Plugin architecture**: User-defined field types and sections

## References

- JSON specification: RFC 8259
- IEEE 754 floating point standard
- Big-endian byte ordering (network byte order)
- Null-terminated string format (C-style strings)