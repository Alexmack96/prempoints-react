/**
 * Each club's colours, used to draw a stand-in crest until a real badge is
 * dropped into client/public/badges.
 *
 * Club crests are trademarks owned by the clubs, so they are not committed
 * here — source them from each club's media pack and save as
 * client/public/badges/{slug}.png. The slug is the team name lowercased with
 * spaces as hyphens: "Manchester City" becomes manchester-city.png. Drop a file
 * in and it appears; there is no list to update.
 */
export type ClubColours = { primary: string; secondary: string };

export const clubColours: Record<string, ClubColours> = {
  arsenal: { primary: '#EF0107', secondary: '#FFFFFF' },
  'aston-villa': { primary: '#95BFE5', secondary: '#670E36' },
  bournemouth: { primary: '#DA291C', secondary: '#000000' },
  brentford: { primary: '#E30613', secondary: '#FFFFFF' },
  brighton: { primary: '#0057B8', secondary: '#FFCD00' },
  chelsea: { primary: '#034694', secondary: '#FFFFFF' },
  coventry: { primary: '#78D0F3', secondary: '#1D1D3A' },
  'crystal-palace': { primary: '#1B458F', secondary: '#C4122E' },
  everton: { primary: '#003399', secondary: '#FFFFFF' },
  fulham: { primary: '#1B1B1B', secondary: '#FFFFFF' },
  hull: { primary: '#F5A12D', secondary: '#000000' },
  ipswich: { primary: '#0044A9', secondary: '#FFFFFF' },
  'leeds-united': { primary: '#1D428A', secondary: '#FFCD00' },
  liverpool: { primary: '#C8102E', secondary: '#00B2A9' },
  'manchester-city': { primary: '#6CABDD', secondary: '#1C2C5B' },
  'manchester-united': { primary: '#DA291C', secondary: '#FBE122' },
  newcastle: { primary: '#241F20', secondary: '#FFFFFF' },
  'nottingham-forest': { primary: '#DD0000', secondary: '#FFFFFF' },
  sunderland: { primary: '#EB172B', secondary: '#FFFFFF' },
  tottenham: { primary: '#132257', secondary: '#FFFFFF' },
};

/**
 * Team names as they are actually stored, mapped to the badge file that exists.
 *
 * The badge URL is derived from the team's name, so the two line up only while
 * whoever typed the team into the admin screen happened to use the same words
 * the file is named after. "Man Utd" and "Manchester United" are the same club
 * and only one of them is a filename, so the short form asks the server for
 * /badges/man-utd.png, gets a 404, and the board falls back to initials.
 *
 * Rather than rename the files to match one spelling, or forbid the short forms
 * people actually type, the aliases live here. Keys are already slugified, so a
 * new spelling is one line with no thought about case or punctuation.
 */
const BADGE_ALIASES: Record<string, string> = {
  'man-utd': 'manchester-united',
  'man-united': 'manchester-united',
  'manchester-utd': 'manchester-united',
  'man-city': 'manchester-city',
  'man-c': 'manchester-city',
  spurs: 'tottenham',
  'tottenham-hotspur': 'tottenham',
  'nottm-forest': 'nottingham-forest',
  forest: 'nottingham-forest',
  'newcastle-united': 'newcastle',
  'newcastle-utd': 'newcastle',
  'brighton-hove-albion': 'brighton',
  'brighton-and-hove-albion': 'brighton',
  'afc-bournemouth': 'bournemouth',
  leeds: 'leeds-united',
  'ipswich-town': 'ipswich',
  'hull-city': 'hull',
  'coventry-city': 'coventry',
  'crystal-palace-fc': 'crystal-palace',
  'aston-villa-fc': 'aston-villa',
};

const toSlug = (teamName: string) =>
  teamName
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '');

export const slugify = (teamName: string) => {
  const slug = toSlug(teamName);
  return BADGE_ALIASES[slug] ?? slug;
};

export const coloursFor = (teamName: string): ClubColours =>
  clubColours[slugify(teamName)] ?? { primary: '#4B5563', secondary: '#FFFFFF' };

/** "Crystal Palace" gives CP, "Everton" gives EV. */
export const initialsFor = (teamName: string) => {
  const words = teamName.trim().split(/\s+/);
  return words.length > 1
    ? (words[0][0] + words[1][0]).toUpperCase()
    : teamName.slice(0, 2).toUpperCase();
};
