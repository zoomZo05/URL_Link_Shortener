# URL Shortening

This context manages short links, their destination selection, lifecycle, and aggregate access statistics.

## Language

**Short link**:
A stable short code associated with one default destination and optional platform destinations.
_Avoid_: Shortened URL record, redirect record

**Default destination**:
The mandatory destination used when no applicable platform destination exists or when a platform destination is not allowed.
_Avoid_: Original link, fallback link

**Platform destination**:
An optional destination associated with iOS or Android and considered when a visitor accesses a short link from that platform.
_Avoid_: Platform original link, device link

**Allowed platform destination**:
A platform destination whose registrable domain is the same as the default destination's registrable domain.
_Avoid_: Safe URL, trusted link

**Registrable domain**:
The public suffix plus the domain label immediately before it, used as the boundary for allowed platform destinations.
_Avoid_: Host, full domain

**Platform routing**:
The selection of an iOS, Android, or default destination for a visitor when the short link is accessed.
_Avoid_: Device redirect

**Custom alias**:
An optional, user-chosen short code that identifies a short link and is case-sensitive.
_Avoid_: Custom URL, custom destination

**Active link**:
A short link that can redirect visitors and record successful access.

**Disabled link**:
A retained short link that cannot redirect visitors but remains available for statistics and later reactivation.

**Deleted link**:
A soft-deleted short link retained for historical purposes and excluded from normal use and listing.

**Access statistics**:
The aggregate click count, creation time, and most recent access time for a short link.
_Avoid_: Platform analytics
