# WDB Field Type Reference

## Overview

WDB files store structured data using a compact binary format. Fields are encoded with type prefixes and bit widths to minimize file size while maintaining precision.

## Field Type Encoding System

### Encoding Pattern

```
[prefix][bitcount][optional_name]

Examples:
  i8health     → 8-bit signed integer named "health"
  u32id        → 32-bit unsigned integer named "id"
  f16speed     → 16-bit float named "speed"
  s4type       → 4-bit string array index named "type" [XIII2LR only]
```

### Type Prefix Characters

| Prefix | Type | Description | Storage Method |
|--------|------|-------------|----------------|
| `i` | Signed Integer | Two's complement signed integer | Bitpacked or full 32-bit |
| `u` | Unsigned Integer | Unsigned integer | Bitpacked or full 32-bit/64-bit |
| `f` | Float | Floating point number | Stored as bitpacked integer |
| `s` | String Array Index | Index into string array | Bitpacked [XIII2LR only] |

## strtypelist Section Values

The `!!strtypelist` section contains type indicators that determine how to interpret field data:

| Value | Type Name     | Description                               | Size                           |
|-------|---------------|-------------------------------------------|--------------------------------|
| 0     | Bitpacked     | Multiple fields packed into 32-bit chunks | Variable (1-32 bits per field) |
| 1     | Float         | IEEE 754 single-precision float           | 4 bytes                        |
| 2     | String Offset | Offset into !!string section              | 4 bytes                        |
| 3     | UInt          | Unsigned integer value                    | 4 or 8 bytes                   |

## Detailed Type Descriptions

### Type 0: Bitpacked Fields

**Purpose**: Pack multiple small fields into 32-bit chunks to save space.

**Bit Count Meanings**:
- `0` or `32`: Use entire 32-bit chunk (no packing)
- `1-31`: Use specified number of bits from chunk

**Packing Order**: MSB-first (Most Significant Bit first)
- Bit 31 → Bit 0 (read left to right in binary representation)

**Supported Subtypes**:
- `i#`: Signed integer (two's complement)
- `u#`: Unsigned integer
- `f#`: Float (stored as integer, requires conversion)
- `s#`: String array index [XIII2LR only]

**Example**:
```
Fields: u4, i8, u12, u8
Binary: [u4][i8][u12][u8] = 32 bits total

Packed into single uint32:
  Bit 31-28: u4  (4 bits)
  Bit 27-20: i8  (8 bits)
  Bit 19-8:  u12 (12 bits)
  Bit 7-0:   u8  (8 bits)

Unpacking Process:
  1. Read 32-bit chunk: 0xAB12C4D5
  2. Convert to binary: 10101011000100101100010011010101
  3. Extract u4:  bits[31:28] = 1010 = 10
  4. Extract i8:  bits[27:20] = 10110001 = -79 (signed)
  5. Extract u12: bits[19:8]  = 001001011000 = 600
  6. Extract u8:  bits[7:0]   = 11010101 = 213
```

**Special Case - Field Spanning**:
If a field requires more bits than remain in the current chunk, it's deferred to the next chunk.

```
Current chunk: 32 bits remaining
Next field: u16 (needs 16 bits) → OK, process
Next field: u20 (needs 20 bits) → Only 16 bits remain
  → Defer u20 to next 32-bit chunk
  → Decrement field index to retry
```

### Type 1: Float

**Storage**: IEEE 754 single-precision (32-bit) floating point

**Format**:
```
Sign (1 bit) | Exponent (8 bits) | Mantissa (23 bits)
```

**Range**:
- Approximately ±3.4 × 10³⁸
- Precision: ~7 decimal digits

**Example**:
```
JSON: "speed": 5.5
Binary: 0x40B00000 (big-endian)
  = 01000000101100000000000000000000 (binary)
  = Sign:0, Exp:129, Mantissa:2621440
  = 5.5 (decimal)
```

**Note**: Bitpacked floats (`f#` with # < 32) store a scaled integer representation, not IEEE 754.

### Type 2: String Offset

**Storage**: 32-bit unsigned integer offset into `!!string` section

**String Section Format**:
- Concatenated null-terminated strings (C-style)
- Offset 0 usually points to empty string ("\0")
- Strings are deduplicated (same string = same offset)

**Example**:
```
!!string section:
  Offset 0:  "\0"           (empty string)
  Offset 1:  "warrior\0"    (7 chars + null)
  Offset 9:  "mage\0"       (4 chars + null)
  Offset 14: "thief\0"      (5 chars + null)

Field value: 0x00000001 (offset 1)
  → Lookup at position 1
  → Read until null terminator
  → Result: "warrior"
```

**JSON Representation**:
```json
{
  "record": "player001",
  "class": "warrior"
}
```

### Type 3: UInt

**Storage**: 32-bit or 64-bit unsigned integer

**32-bit UInt**:
- Range: 0 to 4,294,967,295
- Default for all `u#` fields at this level

**64-bit UInt**:
- Range: 0 to 18,446,744,073,709,551,615
- Used when field name starts with "u64"
- Takes 8 bytes instead of 4

**Example**:
```
Field: "u32id"
Value: 1000
Binary: 0x000003E8 (big-endian, 4 bytes)

Field: "u64largeId"
Value: 5000000000
Binary: 0x000000012A05F200 (big-endian, 8 bytes)
```

## Field Type Extraction Algorithm

### Bitpacked Field Extraction (Type 0)

```
1. Read 32-bit chunk from record data
2. Convert to binary string (32 characters)
3. Set bit index = 32 (start from MSB)
4. For each field in bitpacked group:
   a. Get field prefix (i, u, f, s)
   b. Get field bit count
   c. If bit count == 0 or 32:
      - Use entire 32-bit chunk
      - Set remaining bits = 0
   d. Else if bit count > remaining bits:
      - Defer field to next chunk
      - Move to next 32-bit chunk
   e. Else:
      - Move bit index back by bit count
      - Extract bits from [bit index : bit index + bit count]
      - Convert to appropriate type:
        * i: Two's complement to signed int
        * u: Binary to unsigned int
        * f: Binary to int (stored as int)
        * s: Binary to uint, lookup in strArray dict
      - Decrement remaining bits
5. Move to next field or next chunk
```

### Float Field Extraction (Type 1)

```
1. Read 4 bytes from record data (big-endian)
2. Convert to IEEE 754 float
3. Write to JSON as number
```

### String Offset Field Extraction (Type 2)

```
1. Read 4 bytes from record data (big-endian) → offset
2. Seek to position [offset] in !!string section
3. Read bytes until null terminator (\0)
4. Convert bytes to string (UTF-8)
5. Write to JSON as string
```

### UInt Field Extraction (Type 3)

```
1. Check if field name starts with "u64":
   a. Yes: Read 8 bytes (big-endian) → uint64
   b. No: Read 4 bytes (big-endian) → uint32
2. Write to JSON as number
```

## Field Type Conversion Algorithm (JSON → WDB)

### Bitpacked Field Conversion

```
1. Group fields by strtypelist position
2. For each bitpacked group:
   a. Initialize 32-bit accumulator = 0
   b. Initialize bit position = 31 (MSB)
   c. For each field:
      - Get value from JSON
      - Get bit count from field name
      - Convert value to binary representation
      - Shift accumulator left by bit count
      - OR value into accumulator
      - Decrement bit position
   d. Write accumulator as 4-byte big-endian uint
```

### Float Field Conversion

```
1. Get float value from JSON
2. Convert to IEEE 754 binary (4 bytes)
3. Reverse bytes (little-endian → big-endian)
4. Write 4 bytes
```

### String Field Conversion

```
1. Get string value from JSON
2. Check if string exists in string pool:
   a. Yes: Get existing offset
   b. No: Add to pool, get new offset
3. Convert offset to 4-byte big-endian uint
4. Write 4 bytes
```

### UInt Field Conversion

```
1. Get number value from JSON
2. Check field name for "u64":
   a. u64: Convert to 8-byte big-endian uint64
   b. Other: Convert to 4-byte big-endian uint32
3. Write bytes
```

## String Array System [XIII2LR Only]

### Purpose
Efficiently store enumerated string values (e.g., item types, character classes).

### Structure

**Three Sections**:
1. `!!strArray`: Packed string data (null-terminated)
2. `!!strArrayInfo`: Metadata (offsets per value, bits per offset)
3. `!!strArrayList`: List of field names using string arrays

### Encoding

**String Array Field** (`s#`):
- Part of bitpacked data (type 0)
- Stores index into string array, not offset
- Bit count determines maximum array size (2^n entries)

**Example**:
```
Field: s4type (4 bits = max 16 unique values)
String array for "type":
  Index 0: "warrior"
  Index 1: "mage"
  Index 2: "thief"
  Index 3: "archer"

Record data: bitpacked field contains value 2 (4 bits)
Lookup: strArrayDict["s4type"][2] = "thief"
JSON: "type": "thief"
```

### Offset Indirection

String arrays use bitpacked offsets for space efficiency.

**Metadata** (`!!strArrayInfo`):
- `offsetsPerValue`: Number of offset entries per string
- `bitsPerOffset`: Bit width for each offset value

**Example**:
```
offsetsPerValue: 2
bitsPerOffset: 12

Array with 4 strings → 8 offsets total (4 × 2)
Each offset: 12 bits
Total: 96 bits = 12 bytes (packed)

Offset data (binary): [offset0][offset1][offset2][offset3][offset4][offset5][offset6][offset7]
Each offset points to position in !!strArray section
```

**Reconstruction**:
1. Unpack bitpacked offsets using bitsPerOffset
2. For each string (offsetsPerValue entries):
   - Read offset[n] and offset[n+1]
   - String length = offset[n+1] - offset[n]
   - String position = offset[n]
   - Extract string from !!strArray at position
3. Build dictionary: field name → string list

## Common Pitfalls and Edge Cases

### 1. Bitpacking Field Order
❌ **Wrong**: Process fields LSB-first (bit 0 → bit 31)
✅ **Correct**: Process fields MSB-first (bit 31 → bit 0)

### 2. Signed Integer Representation
❌ **Wrong**: Interpret signed int as unsigned, then subtract
✅ **Correct**: Use two's complement conversion

```
Binary: 11111111 (8 bits)
Wrong:  255 (unsigned)
Correct: -1 (signed, two's complement)
```

### 3. Byte Order (Endianness)
❌ **Wrong**: Read bytes as little-endian
✅ **Correct**: WDB uses big-endian (network byte order)

```
Value: 0x12345678
Little-endian bytes: 78 56 34 12
Big-endian bytes:    12 34 56 78 ← Correct for WDB
```

### 4. String Pool Offsets
❌ **Wrong**: Offset is number of strings
✅ **Correct**: Offset is byte position in section

```
!!string section:
  Position 0: "\0"
  Position 1: "test\0"
  Position 6: "hello\0"

Offset 6 → "hello" (not 6th string)
```

### 5. Zero Bit Count
❌ **Wrong**: 0 bits = field doesn't exist
✅ **Correct**: 0 or 32 bits = use full 32-bit chunk

```
Field: i0value or i32value
Both mean: Use all 32 bits (not bitpacked with others)
```

### 6. String Array vs String Offset
❌ **Wrong**: Treat s# field like type 2 (string offset)
✅ **Correct**: s# is bitpacked index (type 0), requires strArray lookup

```
Type 2 (string offset): Direct offset into !!string section
Type 0 s# (strArray):   Index into strArray dictionary
```

## XIII vs XIII2LR Field Differences

| Feature | XIII | XIII2LR |
|---------|------|---------|
| **String Arrays** | Not supported | `s#` prefix supported |
| **Field Definitions** | Hardcoded in WDBDicts.cs | Dynamic from `!structitem` |
| **Generic Fields** | Used for unknown files | Never used (all files "known") |
| **u64 Support** | Yes | Not observed (may support) |
| **Float Bitpacking** | Yes (`f#`) | Yes (`f#`) |

## Field Type Quick Reference

```
┌─────────────┬──────────────┬───────────────┬──────────────┐
│ Field Name  │ strtypelist  │ Bit Width     │ JSON Type    │
├─────────────┼──────────────┼───────────────┼──────────────┤
│ i8health    │ 0 (bitpack)  │ 8 bits        │ number       │
│ u16id       │ 0 (bitpack)  │ 16 bits       │ number       │
│ f32speed    │ 1 (float)    │ 32 bits       │ number       │
│ name        │ 2 (string)   │ 32 bits       │ string       │
│ u32count    │ 3 (uint)     │ 32 bits       │ number       │
│ u64largeId  │ 3 (uint)     │ 64 bits       │ number       │
│ s4type      │ 0 (bitpack)  │ 4 bits (idx)  │ string       │
│ i0fullInt   │ 0 (bitpack)  │ 32 bits       │ number       │
└─────────────┴──────────────┴───────────────┴──────────────┘
```

## Validation Rules

### Field Name Validation
- Must start with valid prefix: i, u, f, s
- Must have numeric bit count after prefix
- Bit count range: 0-64 (0 treated as 32 for bitpacked)

### Value Range Validation
- Signed int: -(2^(n-1)) to 2^(n-1) - 1
- Unsigned int: 0 to 2^n - 1
- Float: IEEE 754 range
- String array index: 0 to (size of array - 1)

### Bitpacking Validation
- Sum of bit widths in group should be ≤ 32
- If > 32, must span multiple chunks
- Chunk boundaries must align properly

## Examples

### Example 1: Character Stats

```json
{
  "record": "char001",
  "u8level": 50,
  "i16health": 1200,
  "i16mana": 800,
  "f32strength": 95.5,
  "name": "Hero",
  "s4class": "warrior"
}
```

**Binary Breakdown**:
```
Chunk 1 (bitpacked - type 0):
  u8level:   50   = 00110010 (8 bits)
  i16health: 1200 = 0000010010110000 (16 bits)
  i16mana:   800  = 0000001100100000 (16 bits)
  → Total: 40 bits → spans 2 chunks (32 + 8)

Chunk 2 (float - type 1):
  f32strength: 95.5 = 0x42BF0000

Chunk 3 (string offset - type 2):
  name: "Hero" → offset 42 = 0x0000002A

Chunk 4 (bitpacked - type 0):
  s4class: "warrior" → index 0 = 0000 (4 bits)
  (remaining 28 bits unused or contain next fields)
```

### Example 2: Item Data

```json
{
  "record": "item_potion",
  "u16itemId": 101,
  "s8rarity": "common",
  "u32price": 50,
  "f32weight": 0.5,
  "description": "Restores HP"
}
```

**strtypelist**: [0, 0, 3, 1, 2]

**Binary Layout**:
```
Field 0 (type 0, bitpacked):
  u16itemId: 101 = 0000000001100101 (16 bits)

Field 1 (type 0, bitpacked):
  s8rarity: index from strArray["s8rarity"]
  Assume "common" is index 0 = 00000000 (8 bits)

Field 2 (type 3, uint):
  u32price: 50 = 0x00000032 (4 bytes)

Field 3 (type 1, float):
  f32weight: 0.5 = 0x3F000000 (4 bytes)

Field 4 (type 2, string offset):
  description: "Restores HP" → offset in !!string section
```

## References

- Two's complement: https://en.wikipedia.org/wiki/Two%27s_complement
- IEEE 754 floating point: https://en.wikipedia.org/wiki/IEEE_754
- Big-endian byte order: https://en.wikipedia.org/wiki/Endianness
- Bitwise operations: https://en.wikipedia.org/wiki/Bitwise_operation