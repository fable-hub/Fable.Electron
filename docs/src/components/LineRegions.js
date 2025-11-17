/**
 * Extracts a specific region of text from a given raw input string based on region start and end markers.
 *
 * @param {string} raw - The raw input string containing region markers and text.
 * @param {string} regionName - The name of the region to extract. This should match the name used in the markers.
 * @returns {string} - The content of the specified region, with leading and trailing whitespace removed.
 */
const GetRegion = (raw, regionName) => {
    const start = new RegExp(`//%${regionName}%START%`);
    const end = new RegExp(`//%${regionName}%END%`);
    const startIndex = raw.search(start);
    const endIndex = raw.search(end);
    return raw
        .slice(raw.indexOf('\n', startIndex) + 1, endIndex)
        .split('\n')
        .filter(line => !/\/\/%[\w-]+%(?:START|END)%/.test(line))
        .join('\n');
        // .trim();
}
export default GetRegion
