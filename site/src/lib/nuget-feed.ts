// The landing page's typed door onto the one picker. WW500 wrote it here; WW506 moved the
// implementation to `scripts/nuget-feed.mjs` and left this.
//
// The move was not tidying. The documentation area is a second npm project with its own build
// and cannot import a source out of this one, so it was rendering the generated version — right
// on the day it is built and a version behind from the next release onward, on the two pages
// carrying a `PackageReference` somebody pastes into a csproj.
//
// Copying the picker across was the alternative and is refused: the comparison is where the
// real failure lives, and a second copy is two things to keep true where the suite asserts one.
// So the implementation sits in `scripts/`, which both builds can read, and this file is the
// types over it — one producer, two consumers, the shape `scripts/product.mjs` already has.
export { feedUrl, latestPublished, fetchLatestPublished } from "../../scripts/nuget-feed.mjs";
