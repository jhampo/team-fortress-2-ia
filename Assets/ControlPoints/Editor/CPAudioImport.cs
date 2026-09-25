using UnityEditor;
using UnityEngine;
namespace FpsStage2 {
public sealed class CPAudioImport:AssetPostprocessor {
 const string Root="Assets/ControlPoints/Resources/PowerhouseAudio/";
 void OnPreprocessAudio(){if(assetPath.StartsWith(Root))Configure((AudioImporter)assetImporter);}
 static void Configure(AudioImporter importer){var settings=importer.defaultSampleSettings;settings.compressionFormat=AudioCompressionFormat.PCM;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.quality=1;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.loadInBackground=false;}
 public static string Reimport(){int n=0;AssetDatabase.StartAssetEditing();try{foreach(string guid in AssetDatabase.FindAssets("t:AudioClip",new[]{Root.TrimEnd('/')})){var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(AudioImporter)AssetImporter.GetAtPath(path);Configure(importer);importer.SaveAndReimport();n++;}}finally{AssetDatabase.StopAssetEditing();}return n+" PCM clips imported, decompressed before use";}
}
}
