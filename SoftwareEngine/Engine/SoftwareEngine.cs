﻿//#define USE_UNSAFE
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenTK;

namespace SoftwareRasterizer.Engine {
	
	public unsafe partial class SoftwareEngine {
		
		#if USE_UNSAFE
		UnmanagedBuffer<int> colourBuffer;
		#else
		int[] colourBuffer;
		#endif	
		
		public SoftwareEngine( int width, int height ) {
			#if USE_UNSAFE
			colourBuffer = new UnmanagedBuffer<int>( width, height );
			#else
			colourBuffer = new int[width * height];
			#endif
			SetDimensions( width, height );
		}
		
		public void Resize( int width, int height ) {
			#if USE_UNSAFE
			colourBuffer.Resize( width, height );
			depthBuffer.Resize( width, height );
			#else
			colourBuffer = new int[width * height];
			#endif
			SetDimensions( width, height );
		}
		
		int width, height;
		int xMax, yMax;
		void SetDimensions( int width, int height ) {
			this.width = width;
			this.height = height;
			xMax = width - 1;
			yMax = height - 1;
		}
		
		Matrix4 world = Matrix4.Identity, view = Matrix4.Identity, proj = Matrix4.Identity;
		Matrix4 worldViewProj = Matrix4.Identity;
		
		public void SetAllMatrices( ref Matrix4 proj, ref Matrix4 view, ref Matrix4 world ) {
			this.proj = proj;
			this.view = view;
			this.world = world;
			worldViewProj = this.world * this.view * this.proj;
		}
		
		public void Clear( FastColour col ) {
			int colARGB = col.ARGB;
			#if USE_UNSAFE
			IntPtr colPtr = colourBuffer.ptr;
			IntPtr depthPtr = depthBuffer.ptr;
			int* colourBufferPtr = (int*)colPtr;
			uint* depthBufferPtr = (uint*)depthPtr;
			for( int i = 0; i < width * height; i++ ) {
				*colourBufferPtr++ = colARGB;
				*depthBufferPtr++ = UInt32.MaxValue;
			}
			#else
			for( int i = 0; i < colourBuffer.Length; i++ ) {
				colourBuffer[i] = colARGB;
			}
			#endif
		}
		
		public void Present( Graphics g ) {
			#if USE_UNSAFE
			using( Bitmap bmp = new Bitmap( width, height, width * 4, PixelFormat.Format32bppRgb, colourBuffer.ptr ) ) {
				g.DrawImage( bmp, 0, 0, width, height );
			}
			#else
			fixed( int* ptr = colourBuffer ) {
				IntPtr colourBufferPtr = (IntPtr)ptr;
				using( Bitmap bmp = new Bitmap( width, height, width * 4, PixelFormat.Format32bppRgb, colourBufferPtr ) ) {
					g.DrawImage( bmp, 0, 0, width, height );
				}
			}
			#endif
		}

		void PutPixel( int x, int y, FastColour col ) {
			int colARGB = col.ARGB;
			
			#if USE_UNSAFE
			IntPtr scan0 = colourBuffer.ptr;
			int* row = (int*)( scan0 + ( y * width ) );
			row[x] = colARGB;
			#else
			colourBuffer[y * width + x] = colARGB;
			#endif
		}

		// Project takes some 3D coordinates and transform them
		// in 2D coordinates using the transformation matrix
		void Project( ref Vector3 coord, out float x, out float y ) {
			Vector4 point = Vector4.Zero;
			Vector4 wCoord = new Vector4( coord.X, coord.Y, coord.Z, 1 );
			Vector4.Transform( ref wCoord, ref worldViewProj, out point );
			point.X /= point.W;
			point.Y /= point.W;
			point.Z /= point.W;
			
			//x = ( point.X * 0.5f + 0.5f ) * width;
			//y = ( point.Y * 0.5f + 0.5f ) * height;
			x = point.X * width + width / 2.0f;
			y = -point.Y * height + height / 2.0f;
		}

		void SetPixelClipped( ref float x, ref float y, ref FastColour col ) {
			if( x >= 0 && y >= 0 && x < width && y < height )  {
				PutPixel( (int)x, (int)y, col );
			}
		}
		
		void SetPixelClipped( ref int x, ref int y, ref FastColour col ) {
			if( x >= 0 && y >= 0 && x < width && y < height )  {
				PutPixel( x, y, col );
			}
		}
		
		public void DrawVertex_Point( Vector3 vertex ) {
			float x = 0, y = 0;
			Project( ref vertex, out x, out y );
			SetPixelClipped( ref x, ref y, ref FastColour.Cyan );
		}
	}
}
