__kernel void device_function( __global int* newScreen, __global int* oldScreen, int width, int height, int counter)
{
	// adapted from inigo quilez - iq/2013
	int idx = get_global_id( 0 );
	int idy = get_global_id( 1 );
	int id = idx + width * idy;
	if (id > width * height || (idx < 1 || idx > width || idy < 1 || idy > height) ) return;
	int col = 0;
	__global int* oldArray = oldScreen;
	__global int* newArray = newScreen;
	if(counter%2==1)
	{
		oldArray = newScreen;
		newArray = oldScreen;
	}
	col += oldArray[id+1]/2147483647;
	col += oldArray[id-1]/2147483647;
	col += oldArray[id+width]/2147483647;
	col += oldArray[id-width]/2147483647;
	col += oldArray[id+1+width]/2147483647;
	col += oldArray[id-1+width]/2147483647;
	col += oldArray[id+1-width]/2147483647;
	col += oldArray[id-1-width]/2147483647;
	if(((col == 2 || col == 3) && oldArray[id]/2147483647 == 1) || (col == 3 && oldArray[id]/2147483647 == 0))
		col = 2147483647;
	else
		col = 0;	
	int2 pos = (int2)(idx,idy);
	newArray[id] = col;
}
