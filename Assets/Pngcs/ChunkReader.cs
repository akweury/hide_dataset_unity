namespace Pngcs
{
	class ChunkReader
	{

		protected EChunkReaderMode mode;
		Chunks.ChunkRaw chunkRaw;

		bool crcCheck; // by default, this is false for SKIP, true elsewhere
		// protected int read = 0;
		// int crcn = 0; // how many bytes have been read from crc 

		public ChunkReader
		(
			int clen ,
			string id ,
			long offsetInPng ,
			EChunkReaderMode mode
		)
		{
			if( mode<0 || id.Length!=4 || clen<0 )
			{
				throw new System.Exception( $"Bad chunk paramenters: { mode }" );
			}
			
			this.mode = mode;
			chunkRaw = new Chunks.ChunkRaw( clen , id , mode==EChunkReaderMode.BUFFER );
			chunkRaw.offset = offsetInPng;
			this.crcCheck = mode==EChunkReaderMode.SKIP ? false : true;// can be changed with setter
		}

	}
}
