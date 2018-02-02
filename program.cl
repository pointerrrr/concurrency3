// #define GLINTEROP
#ifdef GLINTEROP
__kernel void device_function( write_only image2d_t a, read_only image2d_t z )
#else
__kernel void device_function( __global int* newScreen, __global int* oldScreen, int counter)
#endif
{
	// adapted from inigo quilez - iq/2013
	int idx = get_global_id( 0 );
	int idy = get_global_id( 1 );
	
	//int[] oldScreen = get_global_id(2);
	//int[] newScreen = get_global_id(3);
	int id = idx + 512 * idy;
	if (idx > 510 || idx < 2 || idy > 510 || idy < 2) return;
	float2 fragCoord = (float2)( (float)idx, (float)idy ), resolution = (float2)( 512, 512 );
	//float3 col = (float3)( 0.f, 0.f, 0.f );
	int col = 0;
	//col = (float3)(8.f,0.f,0.f);
	__global int* oldArray = 0;
	__global int* newArray = 0;
	if(counter%2==0)
	{
		oldArray = oldScreen;
		newArray = newScreen;
	}
	else
	{
		oldArray = newScreen;
		newArray = oldScreen;
	}
	col += oldArray[id+1]/255;
	col += oldArray[id-1]/255;
	col += oldArray[id+512]/255;
	col += oldArray[id-512]/255;
	col += oldArray[id+1+512]/255;
	col += oldArray[id-1+512]/255;
	col += oldArray[id+1-512]/255;
	col += oldArray[id-1-512]/255;
	if((col == 2 || col == 3) && oldArray[id]/255 == 1)
		col = 255;
	else if (col == 3 && oldArray[id]/255 == 0)
		col = 255;
	else
		col = 0;	
#ifdef GLINTEROP
	int2 pos = (int2)(idx,idy);
	//write_imagef( a, pos, (float4)(col * (1.0f / 16.0f), 1.0f ) );
#else
	/*int r = (int)clamp( 16.0f * col.x, 0.f, 255.f );
	int g = (int)clamp( 16.0f * col.y, 0.f, 255.f );
	int b = (int)clamp( 16.0f * col.z, 0.f, 255.f );*/
	//a[id] = (r << 16) + (g << 8) + b;
	newArray[id] = col;
#endif
}
