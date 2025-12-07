Role: You are a Senior Backend Engineer specializing in .NET 9, Native AOT (Ahead-of-Time compilation), and FFI (Foreign Function Interface) interoperability with C and Dart.

Context: We are migrating a legacy C# tool (FF13 Modding Tools) from .NET 4.x (Windows-only) to .NET 9 (Cross-platform). The goal is to compile this into a Native AOT Shared Library (.dll/.so/.dylib) that can be called directly from Dart or C++ without requiring the .NET Runtime to be installed on the target machine.

Input Data: You have been provided with WDBDicts.cs. This file contains metadata describing the binary schema of game assets using Hungarian Notation variants (e.g., sName = string, fX = float).

Your Objectives:

Phase 1: .NET 9 Migration & Project Setup
Create a new Project: Initialize a new Class Library project targetting .net9.0.

Configure for Native AOT:

Enable <PublishAot>true</PublishAot> in the .csproj.

Enable <IsAotCompatible>true</IsAotCompatible>.

Ensure the output type is a native shared library (<NativeLib>Shared</NativeLib>).

Constraint: Remove any dependencies on System.Windows.Forms or System.Drawing. The code must run on Linux/macOS.

Phase 2: Schema Parsing & Struct Generation
We need to generate interoperable C-compatible structs based on the FieldNames dictionary in WDBDicts.cs.

Create a Generator/Parser: Write a utility within the project that parses the FieldNames dictionary keys and values.

Type Inference Rules: Apply the following regex/logic to the field names to determine the C type:

Prefix s (e.g., sTitle, sResourceName): Map to a fixed-size byte array char[16] (null-terminated).

Prefix f (e.g., fPosX): Map to float.

Prefix i (e.g., iVal, i16...): Map to int32_t.

Prefix u (e.g., u4Category, u1IsStream): Map to uint32_t.

Note: Even if the prefix suggests a smaller bit-width (like u4), align everything to 32-bit (int/uint) for struct alignment simplicity unless specific packing is requested later.

Output Requirement:

Generate a C Header file (game_structs.h) containing typedef struct definitions for every key in FieldNames.

Generate the equivalent C# unsafe structs using [StructLayout(LayoutKind.Sequential)].

Phase 3: The Fixed String Constraint
The user explicitly stated: "assume all strings are fixed to 16 bytes and are null terminated".

In C# Structs: Define string fields as private fixed byte _fieldName[16];.

Helper Property: Create a helper property in the C# struct to convert this fixed byte buffer to/from a C# string (UTF-8 encoding), ensuring it truncates to 15 chars + null terminator upon write.

Phase 4: Exporting the API (Native ABI)
Create a class NativeExports.cs to serve as the entry point for the DLL.

Export Method: Create a method GetStructJson exposed via [UnmanagedCallersOnly(EntryPoint = "get_struct_json")].

Signature: The method should accept:

char* recordId (The type of struct to retrieve, e.g., "Item").

void* dataPtr (Pointer to the binary data to parse).

char* outJsonBuffer (Pointer to write the resulting JSON).

int bufferSize (Size of the output buffer).

Implementation Logic:

Lookup the RecordIDs dictionary to find the schema name.

Cast dataPtr to the specific generated C# struct (using Unsafe.AsRef).

Serialize that struct to JSON.

Copy the JSON string to outJsonBuffer.