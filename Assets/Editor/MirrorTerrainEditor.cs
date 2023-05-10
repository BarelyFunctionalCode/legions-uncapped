using UnityEngine;
using System.Collections;
using UnityEditor;
 
[CustomEditor(typeof(MirrorTerrain))]
public class MirrorTerrainEditor : Editor {
   
    private float [,] lastHeightMap;
   
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        MirrorTerrain terrain = (MirrorTerrain) target as MirrorTerrain;
        if (!terrain.gameObject) {
            return;
        }
        Terrain terComponent = (Terrain) terrain.GetComponent(typeof(Terrain));
       
       
        if (GUILayout.Button("Mirror X-axis")) {
            lastHeightMap = terComponent.terrainData.GetHeights(0,0,terComponent.terrainData.heightmapResolution,terComponent.terrainData.heightmapResolution);
           
            int xLen = terComponent.terrainData.heightmapResolution;
            int yLen = (int) (terComponent.terrainData.heightmapResolution/2f);
           
            float[,] mirrorHeights = terComponent.terrainData.GetHeights(0,0,xLen,yLen);
           
            float[,] newHeights = new float[yLen,xLen];
             
            for(int i=0;i<xLen;i++) {
                for(int j=0;j<yLen;j++) {
                    newHeights[yLen-j-1,xLen-i-1] = mirrorHeights[j,i];
                     
                }
            }
           
            terComponent.terrainData.SetHeights(0,yLen,newHeights);
        }
       
        if (GUILayout.Button("Mirror Y-axis")) {
            lastHeightMap = terComponent.terrainData.GetHeights(0,0,terComponent.terrainData.heightmapResolution,terComponent.terrainData.heightmapResolution);
           
            int xLen = (int) (terComponent.terrainData.heightmapResolution/2f);
            int yLen = terComponent.terrainData.heightmapResolution;
           
            float[,] mirrorHeights = terComponent.terrainData.GetHeights(0,0,xLen,yLen);
            float[,] newHeights = new float[yLen,xLen];
 
             
            for(int i=0;i<xLen;i++) {
                for(int j=0;j<yLen;j++) {
                    newHeights[yLen-j-1,xLen-i-1] = mirrorHeights[j,i];
                     
                }
            }
           
            terComponent.terrainData.SetHeights(xLen,0,newHeights);
        }
       
        if (GUILayout.Button("Undo")) {
            if (lastHeightMap != null) {
                terComponent.terrainData.SetHeights(0,0,lastHeightMap);
            }
        }
    }
}