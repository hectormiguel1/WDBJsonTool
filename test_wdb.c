#include <stdio.h>
#include <stdlib.h>
#include <string.h>

// Include the C API header
#include "wdb_api.h"

// Forward declare the library handle (for dynamic loading)
#if defined(_WIN32)
#include <windows.h>
#define LIB_HANDLE HMODULE
#define LOAD_LIBRARY(path) LoadLibraryA(path)
#define GET_FUNCTION(handle, name) GetProcAddress(handle, name)
#define FREE_LIBRARY(handle) FreeLibrary(handle)
#else
#include <dlfcn.h>
#define LIB_HANDLE void*
#define LOAD_LIBRARY(path) dlopen(path, RTLD_LAZY)
#define GET_FUNCTION(handle, name) dlsym(handle, name)
#define FREE_LIBRARY(handle) dlclose(handle)
#endif

// Function pointers for the dynamically loaded functions
typedef Result (*WDB_ParseFile_Func)(const char*, unsigned char);
typedef void (*WDB_FreeWDBFile_Func)(WDBFileC*);
typedef void (*WDB_FreeString_Func)(char*);
typedef Result (*WDB_WriteFile_Func)(const char*, unsigned char, WDBFileC*);

int main() {
    LIB_HANDLE lib_handle;
    WDB_ParseFile_Func WDB_ParseFile_ptr;
    WDB_FreeWDBFile_Func WDB_FreeWDBFile_ptr;
    WDB_FreeString_Func WDB_FreeString_ptr;
    WDB_WriteFile_Func WDB_WriteFile_ptr;

    // --- Dynamic loading of the shared library ---
    const char* lib_path = "./publish_aot/WDBJsonTool.so"; // Path to the generated shared library
    printf("Loading library from: %s\n", lib_path);
    lib_handle = LOAD_LIBRARY(lib_path);
    if (!lib_handle) {
        fprintf(stderr, "Error loading library: %s\n",
#if defined(_WIN32)
            "Unknown Windows Error" // GetLastError() could be used here
#else
            dlerror()
#endif
        );
        return 1;
    }

    WDB_ParseFile_ptr = (WDB_ParseFile_Func)GET_FUNCTION(lib_handle, "WDB_ParseFile");
    WDB_FreeWDBFile_ptr = (WDB_FreeWDBFile_Func)GET_FUNCTION(lib_handle, "WDB_FreeWDBFile");
    WDB_FreeString_ptr = (WDB_FreeString_Func)GET_FUNCTION(lib_handle, "WDB_FreeString");
    WDB_WriteFile_ptr = (WDB_WriteFile_Func)GET_FUNCTION(lib_handle, "WDB_WriteFile");

    if (!WDB_ParseFile_ptr || !WDB_FreeWDBFile_ptr || !WDB_FreeString_ptr || !WDB_WriteFile_ptr) {
        fprintf(stderr, "Error getting function pointers: %s\n",
#if defined(_WIN32)
            "Unknown Windows Error" // GetLastError() could be used here
#else
            dlerror()
#endif
        );
        FREE_LIBRARY(lib_handle);
        return 1;
    }


    // --- Prepare input for parsing ---
    const char* wdb_file_path = "WDBJsonTool/crystal_fang.wdb";
    unsigned char game_code = ff13; // FFXIII = 0
    WDBFileC* wdb_data = NULL; // Pointer to struct

    printf("Parsing WDB file: %s with game code: %d\n", wdb_file_path, game_code);

    Result result = WDB_ParseFile_ptr(wdb_file_path, game_code);

    if (result.type == Ok) {
        wdb_data = (WDBFileC*)result.payload.data;
        printf("WDB_ParseFile succeeded.\n");
        printf("WDB Name: %s\n", wdb_data->wdbName);
        printf("Record Count: %d\n", wdb_data->recordCount);

        // --- Print some header information ---
        printf("\nHeader Section (%d entries):\n", wdb_data->header.entryCount);
        for (int i = 0; i < wdb_data->header.entryCount; ++i) {
            WDBEntry header_entry = wdb_data->header.entries[i];
            printf("  Key: %s, Type: %d, Value: ", header_entry.key, header_entry.value.type);
            switch (header_entry.value.type) {
                case WDB_VALUE_TYPE_INT: printf("%d\n", header_entry.value.data.int_val); break;
                case WDB_VALUE_TYPE_UINT: printf("%u\n", header_entry.value.data.uint_val); break;
                case WDB_VALUE_TYPE_FLOAT: printf("%f\n", header_entry.value.data.float_val); break;
                case WDB_VALUE_TYPE_STRING: printf("%s\n", header_entry.value.data.string_val); break;
                case WDB_VALUE_TYPE_BOOL: printf("%s\n", header_entry.value.data.bool_val ? "true" : "false"); break;
                case WDB_VALUE_TYPE_INT_ARRAY:
                    printf("[");
                    for (int j = 0; j < header_entry.value.data.int_array_val.count; ++j) {
                        printf("%d%s", header_entry.value.data.int_array_val.items[j], (j == header_entry.value.data.int_array_val.count - 1) ? "" : ", ");
                    }
                    printf("]\n");
                    break;
                case WDB_VALUE_TYPE_UINT_ARRAY:
                    printf("[");
                    for (int j = 0; j < header_entry.value.data.uint_array_val.count; ++j) {
                        printf("%u%s", header_entry.value.data.uint_array_val.items[j], (j == header_entry.value.data.uint_array_val.count - 1) ? "" : ", ");
                    }
                    printf("]\n");
                    break;
                case WDB_VALUE_TYPE_STRING_ARRAY:
                    printf("[");
                    for (int j = 0; j < header_entry.value.data.string_array_val.count; ++j) {
                        printf("'%s'%s", header_entry.value.data.string_array_val.items[j], (j == header_entry.value.data.string_array_val.count - 1) ? "" : ", ");
                    }
                    printf("]\n");
                    break;
                default: printf("Unhandled type\n"); break;
            }
        }


        // --- Print data from the first few records (if any) ---
        printf("\nFirst few Records:\n");
        for (int r = 0; r < wdb_data->recordCount && r < 5; ++r) { // Print up to 5 records
            WDBRecordC record = wdb_data->records[r];
            printf("  Record %d (%d entries):\n", r, record.entryCount);
            for (int i = 0; i < record.entryCount; ++i) {
                WDBEntry record_entry = record.entries[i];
                printf("    Key: %s, Type: %d, Value: ", record_entry.key, record_entry.value.type);
                switch (record_entry.value.type) {
                    case WDB_VALUE_TYPE_INT: printf("%d\n", record_entry.value.data.int_val); break;
                    case WDB_VALUE_TYPE_UINT: printf("%u\n", record_entry.value.data.uint_val); break;
                    case WDB_VALUE_TYPE_FLOAT: printf("%f\n", record_entry.value.data.float_val); break;
                    case WDB_VALUE_TYPE_STRING: printf("%s\n", record_entry.value.data.string_val); break;
                    case WDB_VALUE_TYPE_BOOL: printf("%s\n", record_entry.value.data.bool_val ? "true" : "false"); break;
                    // Add other types as needed
                    default: printf("Unhandled type\n"); break;
                }
            }
        }

        // --- Test writing functionality ---
        const char* output_wdb_file_path = "WDBJsonTool/crystal_fang_output.wdb";
        printf("\nAttempting to write WDB data to: %s\n", output_wdb_file_path);
        
        // Pass wdb_data (which is already a WDBFileC*) directly
        Result write_result = WDB_WriteFile_ptr(output_wdb_file_path, game_code, wdb_data);

        if (write_result.type == Ok) {
            printf("WDB_WriteFile succeeded. Verifying by parsing the newly written file...\n");
            
            Result reparse_result = WDB_ParseFile_ptr(output_wdb_file_path, game_code);

            if (reparse_result.type == Ok) {
                WDBFileC* new_wdb_data = (WDBFileC*)reparse_result.payload.data;
                printf("Successfully re-parsed '%s'. WDB Name: %s, Record Count: %d\n", 
                       output_wdb_file_path, new_wdb_data->wdbName, new_wdb_data->recordCount);
                
                // Free the new data
                // Assuming free_result is not available or handled by manually freeing content
                // wdb_api.h says WDB_FreeWDBFile is available
                // But Result might wrap it? No, WDB_ParseFile returns Result containing pointer.
                // We should probably free the struct pointed to by payload.data.
                WDB_FreeWDBFile_ptr(new_wdb_data);
                
                // If the Result struct itself needs freeing (if it allocated error string), we might need free_result from common.h
                // But typically for Ok result with pointer, we just use the pointer.
                // If Result was allocated on heap by C# and returned by value... 
                // C# returns struct Result by value. It's copied.
                // So no need to free 'reparse_result' itself, just its content.
                
                printf("New WDB data freed.\n");
            } else {
                fprintf(stderr, "Failed to re-parse '%s' after writing. Error: %s (Code: %d)\n", 
                        output_wdb_file_path, 
                        reparse_result.payload.err ? reparse_result.payload.err->error_message : "Unknown",
                        reparse_result.payload.err ? reparse_result.payload.err->error_code : -1);
            }
        } else {
            fprintf(stderr, "WDB_WriteFile failed. Error: %s (Code: %d)\n",
                    write_result.payload.err ? write_result.payload.err->error_message : "Unknown",
                    write_result.payload.err ? write_result.payload.err->error_code : -1);
        }

        // --- Free allocated memory for original data ---
        printf("\nFreeing WDB data...\n");
        WDB_FreeWDBFile_ptr(wdb_data);
        printf("WDB data freed.\n");

    } else {
        fprintf(stderr, "WDB_ParseFile failed. Error: %s (Code: %d)\n",
                result.payload.err ? result.payload.err->error_message : "Unknown",
                result.payload.err ? result.payload.err->error_code : -1);
    }

    // --- Unload the shared library ---
    FREE_LIBRARY(lib_handle);
    printf("Library unloaded.\n");

    return 0;
}