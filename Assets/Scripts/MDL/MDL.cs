﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;


public class MDL
{
    public string Name;
    public MDLHeader header;    
    public List<MDLSkin> skins;
    public List<MDLTexCoord> texCoords;
    public List<MDLTriangle> triangles;
    public List<MDLFrame> frames;
    public GameObject go;
    public Mesh mesh;

    private BinaryReader mdlFile;
    private BSPPalette palette;

    private Vector3 [] renderVerts;
    private int[] renderTris;
    private Vector2[] renderUVs;
        
    public MDL( string fileName )
    {
        Name = fileName;
        palette = new BSPPalette("palette.lmp");
        mdlFile = new BinaryReader( File.Open( "Assets/Resources/Models/" + fileName, FileMode.Open ) );

        header = new MDLHeader( mdlFile );

        LoadSkins( mdlFile );
        LoadTextureCoords( mdlFile );
        LoadTriangles( mdlFile );
        LoadFrames( mdlFile );

        mdlFile.Close();

        BuildMesh();        
    }

    private void LoadSkins( BinaryReader mdlFile )
    {
        skins = new List<MDLSkin>();

        for ( int i = 0; i < header.skinCount; i++ )        
            skins.Add( new MDLSkin( mdlFile, header, palette ) );        
    }

    private void LoadTextureCoords( BinaryReader mdlFile )
    {
        texCoords = new List<MDLTexCoord>();

        for ( int i = 0; i < header.vertCount; i++ )        
            texCoords.Add( new MDLTexCoord( mdlFile ) );        
    }

    private void LoadTriangles( BinaryReader mdlFile )
    {
        triangles = new List<MDLTriangle>();

        for ( int i = 0; i < header.triCount; i++ )
            triangles.Add( new MDLTriangle( mdlFile) );
    }

    private void LoadFrames( BinaryReader mdlFile )
    {
        frames = new List<MDLFrame>();

        for (int i = 0; i < header.frameCount; i++)
            frames.Add( new MDLFrame( mdlFile, header ) );        
    }

    private void BuildMesh()
    {
        renderVerts = new Vector3[ header.triCount * 3 ];
        renderTris = new int[ header.triCount * 3 ];
        renderUVs = new Vector2[ header.triCount * 3 ];

        SetFrame( 0,0,0 );

        go = new GameObject( Name );
        mesh = new Mesh();
        mesh.vertices = renderVerts;
        mesh.uv = renderUVs;
        mesh.triangles = renderTris;
        mesh.RecalculateNormals();
        go.AddComponent<MeshFilter>().mesh = mesh;
        MDLAnimator animator = go.AddComponent<MDLAnimator>();
        animator.mdl = this;
        animator.PlayAnim(0, 6);

        MeshRenderer rend = go.AddComponent<MeshRenderer>();
        rend.material.shader = Shader.Find( "Legacy Shaders/Diffuse" );
        rend.material.mainTexture = skins[ 0 ].texture;
        rend.material.mainTexture.filterMode = FilterMode.Point;     
    }

    // Just getting it working, will clean up when it works
    public void SetFrame( int frame, int nextFrame, float delta )
    {
        if ( mesh != null ) renderVerts = mesh.vertices;

        int index = 0;

        for ( int i = 0; i < header.triCount; i++ )
        {
            for ( int v = 0; v < 3; v++ )
            {
                MDLVert thisVert = frames[ frame ].verts[ triangles[ i ].vertexIndexes[ v ] ];
                MDLVert nextVert = frames[ nextFrame ].verts[ triangles[ i ].vertexIndexes[ v ] ];
                                
                renderVerts[ index ].x = header.scale.x * ( thisVert.v[ 0 ] + delta * ( nextVert.v[ 0 ] - thisVert.v[ 0 ] ) ) + header.translate[ 0 ];
                renderVerts[ index ].y = header.scale.y * ( thisVert.v[ 1 ] + delta * ( nextVert.v[ 1 ] - thisVert.v[ 1 ] ) ) + header.translate[ 1 ];
                renderVerts[ index ].z = header.scale.z * ( thisVert.v[ 2 ] + delta * ( nextVert.v[ 2 ] - thisVert.v[ 2 ] ) ) + header.translate[ 2 ];

                float s = texCoords[ triangles[ i ].vertexIndexes[ v ] ].s;
                float t = texCoords[ triangles[ i ].vertexIndexes[ v ] ].t;

                if ( triangles[ i ].facesFront != 1 && texCoords[ triangles[ i ].vertexIndexes[ v ] ].onSeam != 0 )
                {
                    s += (float)header.skinWidth * 0.5f;
                }

                s = ( s + 0.5f ) / (float)header.skinWidth;
                t = ( t + 0.5f ) / (float)header.skinHeight;

                renderUVs[ index ].x = s;
                renderUVs[ index ].y = t;
                
                index++;
            }

            renderTris[ i * 3 ] = index - 3;
            renderTris[ i * 3 + 1 ] = index - 2;
            renderTris[ i * 3 + 2 ] = index - 1;
        }

        if ( mesh!=null ) mesh.vertices = renderVerts;
    }
}
