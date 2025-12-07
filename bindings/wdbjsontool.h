/*
 * WDBJsonTool Native Library - C Header
 *
 * This header provides C bindings for the WDBJsonTool native library.
 * The library parses WDB files from Final Fantasy XIII, XIII-2, and Lightning Returns.
 *
 * Copyright (C) Surihix 2025
 */

#ifndef WDBJSONTOOL_H
#define WDBJSONTOOL_H

#ifdef __cplusplus
extern "C" {
#endif

/* ============================================================================
 * Type Definitions
 * ============================================================================ */

#ifndef __cplusplus
typedef unsigned char bool;
#define true 1
#define false 0
#endif

/* Calling convention macro */
#define WDB_API

/* ============================================================================
 * Field Types
 * ============================================================================ */

/**
 * WDB field value types matching the WDB strtypelist values
 */
typedef enum WdbFieldType {
    WDB_FIELD_BITPACKED = 0,  /* Bitpacked integer data */
    WDB_FIELD_FLOAT = 1,      /* 32-bit floating point */
    WDB_FIELD_STRING = 2,     /* String (pointer to null-terminated UTF-8) */
    WDB_FIELD_UINT = 3        /* Unsigned integer */
} WdbFieldType;

/* ============================================================================
 * Structures
 * ============================================================================ */

/**
 * A field value that can hold different types (union)
 * Check the Type field to determine which union member to access.
 */
typedef struct WdbFieldValue {
    WdbFieldType Type;
    union {
        int IntValue;
        unsigned int UIntValue;
        float FloatValue;
        const char* StringPtr;  /* Pointer to null-terminated UTF-8 string */
    };
} WdbFieldValue;

/**
 * A named field with its value
 */
typedef struct WdbField {
    char Name[64];          /* Field name (null-terminated UTF-8) */
    WdbFieldValue Value;
} WdbField;

/**
 * A single record containing multiple fields
 */
typedef struct WdbRecord {
    char Name[16];          /* Record name (null-terminated) */
    unsigned int FieldCount;
    WdbField* Fields;       /* Pointer to array of WdbField */
} WdbRecord;

/**
 * Section metadata (for raw section data like !!string, !!typelist, etc.)
 */
typedef struct WdbSection {
    char Name[32];          /* Section name (null-terminated) */
    unsigned int DataSize;
    void* Data;             /* Pointer to raw section data */
} WdbSection;

/**
 * Complete parsed WDB file structure for FFXIII
 */
typedef struct WdbFile {
    char SheetName[64];             /* Sheet name (null-terminated) */
    unsigned int RecordCount;
    unsigned int SectionCount;
    unsigned int FieldDefinitionCount;
    WdbRecord* Records;             /* Pointer to array of WdbRecord */
    WdbSection* Sections;           /* Pointer to array of WdbSection */
    const char** FieldNames;        /* Pointer to array of field name pointers (for known WDBs) */
    unsigned int* StrtypelistValues;    /* Pointer to array of uint (field types) */
    bool IsKnown;                       /* true if known WDB with field names */
} WdbFile;

/**
 * String array entry for XIII-2/LR strArray support
 */
typedef struct WdbStrArrayEntry {
    char Key[64];           /* Field key name */
    unsigned int StringCount;
    const char** Strings;   /* Pointer to array of string pointers */
} WdbStrArrayEntry;

/**
 * Extended WDB file structure for XIII-2/LR with strArray support
 */
typedef struct WdbFileXIII2LR {
    char SheetName[64];
    unsigned int RecordCount;
    unsigned int SectionCount;
    unsigned int FieldDefinitionCount;
    WdbRecord* Records;
    WdbSection* Sections;
    const char** FieldNames;
    int* StrtypelistValues;     /* Note: signed int for XIII-2/LR */
    bool HasStrArraySection;
    unsigned int StrArrayEntryCount;
    WdbStrArrayEntry* StrArrayEntries;
} WdbFileXIII2LR;

/* ============================================================================
 * JSON Export Functions
 * ============================================================================ */

/**
 * Extract WDB to JSON file for FFXIII
 *
 * @param wdbFilePath   Path to the .wdb file (null-terminated UTF-8 string)
 * @param ignoreKnown   If true, ignores known field names
 * @return              0 on success, non-zero on error
 */
WDB_API int wdb_extract_json_xiii(const char* wdbFilePath, bool ignoreKnown);

/**
 * Convert JSON file to WDB for FFXIII
 *
 * @param jsonFilePath  Path to the .json file (null-terminated UTF-8 string)
 * @return              0 on success, non-zero on error
 */
WDB_API int wdb_convert_json_xiii(const char* jsonFilePath);

/**
 * Extract WDB to JSON file for FFXIII-2 and Lightning Returns
 *
 * @param wdbFilePath   Path to the .wdb file (null-terminated UTF-8 string)
 * @return              0 on success, non-zero on error
 */
WDB_API int wdb_extract_json_xiii2lr(const char* wdbFilePath);

/**
 * Convert JSON file to WDB for FFXIII-2 and Lightning Returns
 *
 * @param jsonFilePath  Path to the .json file (null-terminated UTF-8 string)
 * @return              0 on success, non-zero on error
 */
WDB_API int wdb_convert_json_xiii2lr(const char* jsonFilePath);

/* ============================================================================
 * Native Struct Functions
 * ============================================================================ */

/**
 * Parse WDB file and return native struct for FFXIII
 *
 * @param wdbFilePath   Path to the .wdb file (null-terminated UTF-8 string)
 * @param ignoreKnown   If true, ignores known field names
 * @return              Pointer to WdbFile struct, or NULL on error.
 *                      Caller must free with wdb_free_xiii()
 */
WDB_API WdbFile* wdb_parse_xiii(const char* wdbFilePath, bool ignoreKnown);

/**
 * Parse WDB file and return native struct for FFXIII-2/LR
 *
 * @param wdbFilePath   Path to the .wdb file (null-terminated UTF-8 string)
 * @return              Pointer to WdbFileXIII2LR struct, or NULL on error.
 *                      Caller must free with wdb_free_xiii2lr()
 */
WDB_API WdbFileXIII2LR* wdb_parse_xiii2lr(const char* wdbFilePath);

/**
 * Free a WdbFile structure allocated by wdb_parse_xiii
 *
 * @param wdbFile       Pointer to WdbFile to free
 */
WDB_API void wdb_free_xiii(WdbFile* wdbFile);

/**
 * Free a WdbFileXIII2LR structure allocated by wdb_parse_xiii2lr
 *
 * @param wdbFile       Pointer to WdbFileXIII2LR to free
 */
WDB_API void wdb_free_xiii2lr(WdbFileXIII2LR* wdbFile);

#ifdef __cplusplus
}
#endif

#endif /* WDBJSONTOOL_H */
