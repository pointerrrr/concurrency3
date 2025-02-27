﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Cloo;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.IO;

namespace Template {

    class Game
    {
	    // when GLInterop is set to true, the fractal is rendered directly to an OpenGL texture
	    bool GLInterop = false;
	    // load the OpenCL program; this creates the OpenCL context
	    static OpenCLProgram ocl = new OpenCLProgram( "../../program.cl" );
	    // find the kernel named 'device_function' in the program
	    OpenCLKernel kernel = new OpenCLKernel( ocl, "device_function" );
        // create a regular buffer; by default this resides on both the host and the device
        OpenCLBuffer<int> buffer;
        OpenCLBuffer<int> oldScreen;
        int counter = 0;
        // create an OpenGL texture to which OpenCL can send data
        OpenCLImage<int> image = new OpenCLImage<int>( ocl, 512, 512 );
	    public Surface screen;

        int pw, ph; // note: pw is in uints; width in bits is 32 this value.
        bool lastLButtonState; // for mouse input
        int xoffset, yoffset, dragXStart, dragYStart, offsetXStart, offsetYStart; // for mouse input

        Stopwatch timer = new Stopwatch();
	    //float t = 21.5f;
	    public void Init()
	    {
            int[] parsedArray = parser();
            buffer = new OpenCLBuffer<int>(ocl, ph * pw);
            oldScreen = new OpenCLBuffer<int>(ocl, ph * pw);
            for (int i = 0; i < oldScreen.Length; i++)
                oldScreen[i] = parsedArray[i];
            oldScreen.CopyToDevice();
            if (GLInterop) kernel.SetArgument(0, image);
            else kernel.SetArgument(0, buffer);
            kernel.SetArgument(1, oldScreen);
            kernel.SetArgument(2, pw);
            kernel.SetArgument(3, ph);
        }
        
        public void Tick()
	    {
		    GL.Finish();
		    // clear the screen
		    screen.Clear( 0 );
            // do opencl stuff
            kernel.SetArgument(4, counter++);
            // execute kernel
            int size = (int)(Math.Ceiling(Math.Log(Math.Max(pw, ph), 2)));
            long [] workSize = { 1 << size, 1 << size };
		    long [] localSize = { 32, 4 };
		    if (GLInterop)
		    {
			    // INTEROP PATH:
			    // Use OpenCL to fill an OpenGL texture; this will be used in the
			    // Render method to draw a screen filling quad. This is the fastest
			    // option, but interop may not be available on older systems.
			    // lock the OpenGL texture for use by OpenCL
			    kernel.LockOpenGLObject( image.texBuffer );
			    // execute the kernel
			    kernel.Execute( workSize, localSize );
			    // unlock the OpenGL texture so it can be used for drawing a quad
			    kernel.UnlockOpenGLObject( image.texBuffer );
		    }
		    else
		    {
			    // NO INTEROP PATH:
			    // Use OpenCL to fill a C# pixel array, encapsulated in an
			    // OpenCLBuffer<int> object (buffer). After filling the buffer, it
			    // is copied to the screen surface, so the template code can show
			    // it in the window.
			    // execute the kernel
			    kernel.Execute( workSize, localSize );
			    // get the data from the device to the host
			    buffer.CopyFromDevice();
			    // plot pixels using the data on the host
			    for( int y = 0; y < 512; y++ ) for( int x = 0; x < 512; x++ )
			    {
				    screen.pixels[(x) + (y) * screen.width] = buffer[(x+xoffset) + (y+yoffset) * pw];
			    }
		    }
        }

        // code taken from from c# gol template
        int[] parser()
        {
            int[] result = null;
            StreamReader sr = new StreamReader("../../assets/turing_js_r.rle");
            uint state = 0, n = 0, x = 0, y = 0;
            while (true)
            {
                String line = sr.ReadLine();
                if (line == null) break; // end of file
                int pos = 0;
                if (line[pos] == '#') continue; /* comment line */
                else if (line[pos] == 'x') // header
                {
                    String[] sub = line.Split(new char[] { '=', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    pw = Int32.Parse(sub[1]);
                    ph = Int32.Parse(sub[3]);
                    result = new int[pw * ph];
                }
                else while (pos < line.Length)
                    {
                        Char c = line[pos++];
                        if (state == 0) if (c < '0' || c > '9') { state = 1; n = Math.Max(n, 1); } else n = (uint)(n * 10 + (c - '0'));
                        if (state == 1) // expect other character
                        {
                            if (c == '$') { y += n; x = 0; } // newline
                            else if (c == 'o') for (int i = 0; i < n; i++) result[x++ + y * pw] = 2147483647; else if (c == 'b') x += n;
                            state = n = 0;
                        }
                    }
            }
            return result;
        }
        public void Render() 
	    {
		    // use OpenGL to draw a quad using the texture that was filled by OpenCL
		    if (GLInterop)
		    {
			    GL.LoadIdentity();
			    GL.BindTexture( TextureTarget.Texture2D, image.OpenGLTextureID );
			    GL.Begin( PrimitiveType.Quads );
			    GL.TexCoord2( 0.0f, 1.0f ); GL.Vertex2( -1.0f, -1.0f );
			    GL.TexCoord2( 1.0f, 1.0f ); GL.Vertex2(  1.0f, -1.0f );
			    GL.TexCoord2( 1.0f, 0.0f ); GL.Vertex2(  1.0f,  1.0f );
			    GL.TexCoord2( 0.0f, 0.0f ); GL.Vertex2( -1.0f,  1.0f );
			    GL.End();
		    }
	    }

        // code taken from from c# gol template
        public void SetMouseState(int x, int y, bool pressed)
        {
            if (pressed)
            {
                if (lastLButtonState)
                {
                    int deltax = x - dragXStart, deltay = y - dragYStart;
                    xoffset = offsetXStart - deltax;
                    yoffset = offsetYStart - deltay;
                    if (xoffset > pw - 512)
                        xoffset = pw - 512;
                    if (yoffset > ph - 512)
                        yoffset = ph - 512;
                    if (xoffset < 0)
                        xoffset = 0;
                    if (yoffset < 0)
                        yoffset = 0;
                }
                else
                {
                    dragXStart = x;
                    dragYStart = y;
                    offsetXStart = xoffset;
                    offsetYStart = yoffset;
                    lastLButtonState = true;
                }
            }
            else lastLButtonState = false;
        }
    }
} // namespace Template