#ifndef WDB_API_H
#define WDB_API_H

#ifdef __cplusplus
extern "C" {
#endif

// Enum for WDB value types
typedef enum WDBValueType {
    WDB_VALUE_TYPE_INT,
    WDB_VALUE_TYPE_UINT,
    WDB_VALUE_TYPE_FLOAT,
    WDB_VALUE_TYPE_STRING,
    WDB_VALUE_TYPE_BOOL,
    WDB_VALUE_TYPE_INT_ARRAY,
    WDB_VALUE_TYPE_UINT_ARRAY,
    WDB_VALUE_TYPE_STRING_ARRAY,
    WDB_VALUE_TYPE_UNKNOWN // Fallback for unhandled types
} WDBValueType;

// Union to hold different WDB value data types
typedef struct WDBValue {
    WDBValueType type;
    union {
        int int_val;
        unsigned int uint_val;
        float float_val;
        char* string_val;
        int bool_val; // C doesn't have a direct bool type, using int

        struct {
            int* items;
            int count;
        } int_array_val;
        struct {
            unsigned int* items;
            int count;
        } uint_array_val;
        struct {
            char** items;
            int count;
        } string_array_val;
    } data;
} WDBValue;

// Struct for a key-value pair in WDB sections/records
typedef struct WDBEntry {
    char* key;
    WDBValue value;
} WDBEntry;

// Struct to represent a WDB section (e.g., header)
typedef struct WDBSectionC {
    WDBEntry* entries;
    int entryCount;
} WDBSectionC;

// Struct to represent a single WDB record
typedef struct WDBRecordC {
    WDBEntry* entries;
    int entryCount;
} WDBRecordC;

// Main struct to hold the parsed WDB file data
typedef struct WDBFileC {
    char* wdbName;
    WDBSectionC header;
    WDBRecordC* records;
    int recordCount;
} WDBFileC;


// API functions (will be defined in the C# library and exported)
// Function to initialize the WDB parser
void WDB_Initialize();

// Function to parse a WDB file and return its data
// Returns 0 on success, -1 on failure
int WDB_ParseFile(const char* filePath, const char* gameCode, WDBFileC* outWDBFile);

// Function to free memory allocated for a WDBFileC structure
void WDB_FreeWDBFile(WDBFileC* wdbFile);

// Function to free a string allocated by the library
void WDB_FreeString(char* str);


#ifdef __cplusplus
}
#endif

#endif // WDB_API_H
