using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using IL2CPU.Debug.Symbols.Metadata;
using IL2CPU.Debug.Symbols.Pdb;

namespace IL2CPU.Debug.Symbols
{
    public class DebugSymbolReader
    {
        private static string mCurrentFile;
        private static DebugSymbolReader mCurrentDebugSymbolReader;

        private readonly PEReader mPEReader;
        private readonly MetadataReader mMetadataReader;
        private readonly PdbSymbolReader mSymbolReader;

        private DebugSymbolReader(string aFilePath)
        {
            mPEReader = new PEReader(File.OpenRead(aFilePath), PEStreamOptions.PrefetchEntireImage);
            mSymbolReader = OpenAssociatedSymbolFile(aFilePath, mPEReader);
            mMetadataReader = mPEReader.GetMetadataReader();
        }

        internal static DebugSymbolReader GetReader(string aFilePath)
        {
            if (File.Exists(aFilePath))
            {
                if (mCurrentDebugSymbolReader != null && mCurrentFile == aFilePath)
                {
                    return mCurrentDebugSymbolReader;
                }

                mCurrentDebugSymbolReader = new DebugSymbolReader(aFilePath);

                if (mCurrentDebugSymbolReader.mPEReader.HasMetadata)
                {
                    mCurrentFile = aFilePath;

                    return mCurrentDebugSymbolReader;
                }
            }

            return null;
        }

        private PdbSymbolReader OpenAssociatedSymbolFile(string peFilePath, PEReader peReader)
        {
            // Assume that the .pdb file is next to the binary
            var pdbFilename = Path.ChangeExtension(peFilePath, ".pdb");
            string searchPath = "";

            if (!File.Exists(pdbFilename))
            {
                pdbFilename = null;

                // If the file doesn't exist, try the path specified in the CodeView section of the image
                foreach (DebugDirectoryEntry debugEntry in peReader.ReadDebugDirectory())
                {
                    if (debugEntry.Type != DebugDirectoryEntryType.CodeView)
                    {
                        continue;
                    }

                    string candidateFileName = peReader.ReadCodeViewDebugDirectoryData(debugEntry).Path;
                    if (Path.IsPathRooted(candidateFileName) && File.Exists(candidateFileName))
                    {
                        pdbFilename = candidateFileName;
                        searchPath = Path.GetDirectoryName(pdbFilename);
                        break;
                    }
                }

                if (pdbFilename == null)
                {
                    return null;
                }
            }

            // Try to open the symbol file as portable pdb first
            PdbSymbolReader reader = PortablePdbSymbolReader.TryOpen(pdbFilename, MetadataHelper.GetMetadataStringDecoder());
            if (reader == null)
            {
                // Fallback to the diasymreader for non-portable pdbs
                reader = UnmanagedPdbSymbolReader.TryOpenSymbolReaderForMetadataFile(peFilePath, searchPath);
            }

            return reader;
        }

        public static DebugInfo.SequencePoint[] GetSequencePoints(string aAssemblyPath, int aMetadataToken)
        {
            var xSequencePoints = new List<DebugInfo.SequencePoint>();
            try
            {
                var xReader = MetadataHelper.TryGetReader(aAssemblyPath);
                if (xReader == null)
                {
                    return xSequencePoints.ToArray();
                }

                var xMethodDebugInfoHandle = MetadataTokens.MethodDebugInformationHandle(aMetadataToken);
                if (!xMethodDebugInfoHandle.IsNil)
                {
                    var xDebugInfo = xReader.GetMethodDebugInformation(xMethodDebugInfoHandle);
                    var xDebugInfoSequencePoints = xDebugInfo.GetSequencePoints();
                    foreach (var xSequencePoint in xDebugInfoSequencePoints)
                    {
                        string xDocumentName = string.Empty;
                        if (!xSequencePoint.Document.IsNil)
                        {
                            var xDocument = xReader.GetDocument(xSequencePoint.Document);
                            if (!xDocument.Name.IsNil)
                            {
                                xDocumentName = xReader.GetString(xDocument.Name);
                            }
                        }

                        xSequencePoints.Add(new DebugInfo.SequencePoint
                        {
                            Document = xDocumentName,
                            ColStart = xSequencePoint.StartColumn,
                            ColEnd = xSequencePoint.EndColumn,
                            LineStart = xSequencePoint.StartLine,
                            LineEnd = xSequencePoint.EndLine,
                            Offset = xSequencePoint.Offset
                        });
                    }

                }
            }
            catch (Exception)
            {
            }

            return xSequencePoints.ToArray();
        }

        public static MethodBodyBlock GetMethodBodyBlock(Module aModule, int aMetadataToken)
        {
            var xMethodDefHandle = MetadataTokens.MethodDefinitionHandle(aMetadataToken);
            if (!xMethodDefHandle.IsNil)
            {
                string xLocation = aModule.Assembly.Location;
                var xReader = GetReader(xLocation);
                var xMethodDefinition = xReader.mMetadataReader.GetMethodDefinition(xMethodDefHandle);
                if (xMethodDefinition.RelativeVirtualAddress > 0)
                {
                    int xRelativeVirtualAddress = xMethodDefinition.RelativeVirtualAddress;
                    return xReader.mPEReader.GetMethodBody(xRelativeVirtualAddress);
                }
            }
            return null;
        }

        public static IList<LocalVariableInfo> GetLocalVariableInfos(MethodBase aMethodBase)
        {
            if(aMethodBase.GetMethodBody() is null)
            {
                return new List<LocalVariableInfo>();
            }
            return aMethodBase.GetMethodBody().LocalVariables;
        }

        public static bool TryGetStaticFieldValue(Module aModule, int aMetadataToken, ref byte[] aBuffer)
        {
            var xAssemblyPath = aModule.Assembly.Location;
            var xMetadataReader = GetReader(xAssemblyPath).mMetadataReader;
            var xPEReader = GetReader(xAssemblyPath).mPEReader;

            var xHandle = (FieldDefinitionHandle)MetadataTokens.Handle(aMetadataToken);

            if (!xHandle.IsNil)
            {
                var xFieldDefinition = xMetadataReader.GetFieldDefinition(xHandle);
                var xRVA = xFieldDefinition.GetRelativeVirtualAddress();

                if (xFieldDefinition.Attributes.HasFlag(FieldAttributes.HasFieldRVA))
                {
                    var xBytes = xPEReader.GetSectionData(xRVA).GetContent();

                    for (int i = 0; i < aBuffer.Length; i++)
                    {
                        aBuffer[i] = xBytes[i];
                    }
                }
            }

            return false;
        }
    }
}
