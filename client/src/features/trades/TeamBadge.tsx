import { useState } from 'react';
import { coloursFor, initialsFor, slugify } from './clubColours';

/**
 * Extensions tried in order. Badges arrive from twenty different club media
 * pages, so they do not all arrive as PNGs — Brighton's is a .webp. Trying a
 * short list beats renaming files by hand and hoping the browser sniffs the
 * content type correctly.
 */
const EXTENSIONS = ['png', 'webp', 'svg'];

/**
 * A club crest, or a stand-in built from the club's colours when we do not have
 * the real badge yet. Falling back rather than showing a broken image means the
 * board looks finished with seventeen badges present or twenty.
 */
export const TeamBadge = ({ teamName, size = 52 }: { teamName: string; size?: number }) => {
  const [attempt, setAttempt] = useState(0);
  const { primary, secondary } = coloursFor(teamName);

  if (attempt >= EXTENSIONS.length) {
    return (
      <div
        className="flex shrink-0 items-center justify-center rounded-full font-bold"
        style={{
          width: size,
          height: size,
          background: `linear-gradient(135deg, ${primary} 0%, ${primary} 55%, ${secondary} 55%, ${secondary} 100%)`,
          color: secondary.toUpperCase() === '#FFFFFF' ? '#FFFFFF' : primary,
          fontSize: size * 0.32,
          textShadow: '0 1px 2px rgba(0,0,0,.45)',
        }}
        aria-hidden
      >
        {initialsFor(teamName)}
      </div>
    );
  }

  return (
    <img
      src={`/badges/${slugify(teamName)}.${EXTENSIONS[attempt]}`}
      alt=""
      width={size}
      height={size}
      className="shrink-0 object-contain"
      onError={() => setAttempt((current) => current + 1)}
    />
  );
};
