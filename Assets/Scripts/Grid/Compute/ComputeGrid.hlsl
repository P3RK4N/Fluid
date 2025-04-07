#ifndef COMPUTE_GRID_ENABLED
#define COMPUTE_GRID_ENABLED

// _bucketCapacity is bucketSize (capacity - 1) + count (1)

uint _gridSize;
uint _gridSize2;
uint _gridSize3;
uint _bucketCapacity;
float _bucketRadius;
float _inverseBucketRadius;

RWStructuredBuffer<uint> IndexGrid;             // id -> xyz
RWStructuredBuffer<uint> _offsets;

// Returns valid bucketIndex for current step (step 0 is identity)
// TODO: Think of better way since there MUST NOT be collisions between steps. (For now just use next bucket)
uint _GridHash(uint x, uint step)
{
    return (x + step * 2) % _gridSize3;

    //if (step == 0)
    //    return x % _gridSize3;
    //x += 1;
    //x ^= x * 1664525u;
    //x ^= x * 1013904223u;
    //x *= step * 1664525u;
    //x ^= x >> 16;

    //return x % _gridSize3;
}

// TODO: Optimize? (DONE: Add very bigg number to remove negatives. Works only on small coordinates -> almost all in practice)
uint3 _umod(int3 val, int n)
{
    return uint3(val + n * 1000000) % n;
    //return uint3((val % n + n) % n);
};

// Normalize point coords (Where buckets are spaced by unit of 1)
// Floor normalized point coords
// Wrap floored coords with modulo _gridSize -> bucket coords
uint3 _getBucketCoords(float3 pointCoords)
{
    float3 normalizedPointCoords = pointCoords * _inverseBucketRadius;
    int3 flooredBucketCoords = int3(floor(normalizedPointCoords));
    uint3 wrappedBucketCoords = _umod(flooredBucketCoords, _gridSize);
    return wrappedBucketCoords;
}

static groupshared const uint3 _gridOffsets[27] =
{
    uint3(-1, -1, -1),  // Back-Left-Bottom
    uint3(-1, -1,  0),  // Back-Left
    uint3(-1, -1,  1),  // Back-Left-Top

    uint3(-1,  0, -1),  // Left-Bottom
    uint3(-1,  0,  0),  // Left
    uint3(-1,  0,  1),  // Left-Top

    uint3(-1,  1, -1),  // Front-Left-Bottom
    uint3(-1,  1,  0),  // Front-Left
    uint3(-1,  1,  1),  // Front-Left-Top

    uint3( 0, -1, -1),  // Back-Center-Bottom
    uint3( 0, -1,  0),  // Back-Center
    uint3( 0, -1,  1),  // Back-Center-Top

    uint3( 0,  0, -1),  // Center-Bottom
    uint3( 0,  0,  0),  // Center (Self)
    uint3( 0,  0,  1),  // Center-Top

    uint3( 0,  1, -1),  // Front-Center-Bottom
    uint3( 0,  1,  0),  // Front-Center
    uint3( 0,  1,  1),  // Front-Center-Top

    uint3( 1, -1, -1),  // Back-Right-Bottom
    uint3( 1, -1,  0),  // Back-Right
    uint3( 1, -1,  1),  // Back-Right-Top

    uint3( 1,  0, -1),  // Right-Bottom
    uint3( 1,  0,  0),  // Right
    uint3( 1,  0,  1),  // Right-Top

    uint3( 1,  1, -1),  // Front-Right-Bottom
    uint3( 1,  1,  0),  // Front-Right
    uint3( 1,  1,  1)   // Front-Right-Top
};

// Get bucket seed for point P
uint _getBucketSeed(float3 p)
{
    uint3 bucketCoords = _getBucketCoords(p);
    uint bucketSeed = 
        bucketCoords.x * _gridSize2 +       // x-th slice
        bucketCoords.y * _gridSize +        // y-th row
        bucketCoords.z;                     // z-th cell
    
    return bucketSeed;
}

// Gets 3x3x3 bucket seeds around point P
void _getAdjacentBucketSeeds(float3 p, out uint bucketSeeds[27])
{
    uint3 centerBucketCoords = _getBucketCoords(p);

    [unroll]
    for (int i = 0; i < 27; i++)
    {
        uint3 currentBucketCoords = centerBucketCoords + _gridOffsets[i] + _gridSize;
        currentBucketCoords %= _gridSize;
        
        bucketSeeds[i] =
            currentBucketCoords.x * _gridSize2 +       // x-th slice
            currentBucketCoords.y * _gridSize +        // y-th row
            currentBucketCoords.z;                     // z-th cell
    }
}

// Return is true if there is bucket overflow, otherwise false
// Fills start and end bucket index for iteration
// End index is exclusive
bool _getBucketRange(uint bucketSeed, uint step, out uint bucketStartIndex, out uint bucketEndIndex)
{
    bucketStartIndex = _GridHash(bucketSeed, step) * _bucketCapacity;
    
    uint maxSize = _bucketCapacity - 1;
    uint count = IndexGrid[bucketStartIndex + maxSize];
    
    bucketEndIndex = bucketStartIndex + min(count, maxSize);
    
    return count > maxSize;
}

#define FOREACH_ADJACENT_VALUE_BEGIN(p, val)                                                                \
    [unroll]                                                                                                \
    do                                                                                                      \
    {                                                                                                       \
        uint __adjacentSeeds[27];                                                                           \
        _getAdjacentBucketSeeds(p, __adjacentSeeds);                                                        \
        for (uint __i = 0; __i < 27; __i++)                                                                 \
        {                                                                                                   \
            uint __step = 0;                                                                                \
            bool __continue = true;                                                                         \
            uint __bucketStart, __bucketEnd;                                                                \
            while (__continue)                                                                              \
            {                                                                                               \
                __continue = _getBucketRange(__adjacentSeeds[__i], __step++, __bucketStart, __bucketEnd);   \
                for (uint __j = __bucketStart; __j < __bucketEnd; __j++)                                    \
                {                                                                                           \
                    uint val = IndexGrid[__j];


#define FOREACH_ADJACENT_VALUE_END()   }}}} while (0);

#endif