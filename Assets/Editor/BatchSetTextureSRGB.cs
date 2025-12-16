using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class BatchSetTextureSRGB
{
    [MenuItem("Tools/Batch Set sRGB for Folder")]
    public static void BatchSetSRGBForFolder()
    {
        // 获取选中的文件夹
        string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("请选择一个文件夹");
            return;
        }

        // 获取文件夹下所有图片文件
        string[] imagePaths = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".tga"))
            .ToArray();

        int modifiedCount = 0;

        // 遍历所有图片文件
        foreach (string path in imagePaths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                // 检查当前设置是否需要修改
                if (importer.sRGBTexture != true) // 这里设置为true，可根据需要调整
                {
                    importer.sRGBTexture = true;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                    modifiedCount++;
                }
            }
        }

        Debug.Log($"已完成！共修改了 {modifiedCount} 个纹理的sRGB设置");
    }

    // 验证方法，确保只有选中文件夹时菜单项可用
    [MenuItem("Tools/Batch Set sRGB for Folder", true)]
    public static bool ValidateBatchSetSRGBForFolder()
    {
        return Selection.activeObject != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }
}