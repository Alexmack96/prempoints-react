<#
.SYNOPSIS
    Seeds a season, its gameweeks, the twenty clubs and one day's prices.

.DESCRIPTION
    Drives the API rather than the database. Both endpoints it calls already own
    logic worth not reimplementing in SQL: seednewseason generates the gameweeks
    and the team enrolments in a single transaction, and prices/bulk upserts
    against the unique (TeamId, ValueDate) index. Prices.Mid is a computed column
    besides, so a hand-written INSERT has to know not to touch it.

    Both endpoints require the Administrator role, so you need a token from an
    account whose Users row has Role = 1.

.PARAMETER Token
    A WorkOS access token for an administrator. Get one from the running app:
    sign in, open devtools, Network tab, click any /api/v1 request, and copy the
    value of the Authorization header after "Bearer ".

.PARAMETER BaseUrl
    Defaults to production.

.PARAMETER SeasonName, SeasonStart, SeasonEnd
    The season to create. It must span the days you intend to trade on, because
    handlers look up "the season covering this date" and return 404 when there
    isn't one.

.PARAMETER ValueDate
    The day the prices are for. Defaults to today.

.PARAMETER TeamsOnly / PricesOnly
    Run half of it. Seeding the season is a one-off; prices are weekly.

.EXAMPLE
    ./seed-prod.ps1 -Token 'eyJhbGci...'

.EXAMPLE
    ./seed-prod.ps1 -Token 'eyJhbGci...' -PricesOnly -ValueDate 2026-08-28
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Token,

    [string]$BaseUrl = 'https://prempoints.up.railway.app',

    [string]$SeasonName = '2026/27',
    [datetime]$SeasonStart = '2026-08-14',
    [datetime]$SeasonEnd = '2027-05-23',

    [datetime]$ValueDate = (Get-Date),

    [switch]$TeamsOnly,
    [switch]$PricesOnly
)

$ErrorActionPreference = 'Stop'

# The roster, and the opening spread for each club.
#
# Names are spelled to match the badge files in client/public/badges exactly.
# The badge URL is derived from the team name, so "Man Utd" here would ask for
# /badges/man-utd.png, get a 404, and fall back to drawn initials. Rename a club
# and rename its badge with it.
#
# Bid and Ask are PLACEHOLDERS, ordered by rough strength, and you should
# replace them before anyone trades against them. They are here so the board
# renders with something sane, not because these are the right numbers.
$board = @(
    @{ Team = 'Liverpool';         Bid = 78.0; Ask = 81.0 }
    @{ Team = 'Arsenal';           Bid = 74.0; Ask = 77.0 }
    @{ Team = 'Manchester City';   Bid = 72.0; Ask = 75.0 }
    @{ Team = 'Chelsea';           Bid = 64.0; Ask = 67.0 }
    @{ Team = 'Newcastle';         Bid = 60.0; Ask = 63.0 }
    @{ Team = 'Aston Villa';       Bid = 58.0; Ask = 61.0 }
    @{ Team = 'Manchester United'; Bid = 56.0; Ask = 59.0 }
    @{ Team = 'Tottenham';         Bid = 54.0; Ask = 57.0 }
    @{ Team = 'Brighton';          Bid = 52.0; Ask = 55.0 }
    @{ Team = 'Nottingham Forest'; Bid = 50.0; Ask = 53.0 }
    @{ Team = 'Crystal Palace';    Bid = 48.0; Ask = 51.0 }
    @{ Team = 'Brentford';         Bid = 44.0; Ask = 47.0 }
    @{ Team = 'Fulham';            Bid = 43.0; Ask = 46.0 }
    @{ Team = 'Everton';           Bid = 41.0; Ask = 44.0 }
    @{ Team = 'Bournemouth';       Bid = 40.0; Ask = 43.0 }
    @{ Team = 'Leeds United';      Bid = 34.0; Ask = 37.0 }
    @{ Team = 'Sunderland';        Bid = 32.0; Ask = 35.0 }
    @{ Team = 'Ipswich';           Bid = 28.0; Ask = 31.0 }
    @{ Team = 'Coventry';          Bid = 26.0; Ask = 29.0 }
    @{ Team = 'Hull';              Bid = 24.0; Ask = 27.0 }
)

$headers = @{
    Authorization = "Bearer $Token"
    'Content-Type' = 'application/json'
}

function Invoke-Api {
    param([string]$Path, $Body)

    $uri = "$BaseUrl/api/v1/$Path"
    $json = $Body | ConvertTo-Json -Depth 6

    try {
        return Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body $json
    }
    catch {
        # The API answers ProblemDetails on every failure, and the useful part
        # is in the body rather than the status line PowerShell surfaces.
        $response = $_.Exception.Response
        if ($null -ne $response) {
            $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
            $detail = $reader.ReadToEnd()
            Write-Host "POST $uri failed with $([int]$response.StatusCode)." -ForegroundColor Red
            Write-Host $detail -ForegroundColor DarkGray
        }
        throw
    }
}

if (-not $PricesOnly) {
    Write-Host "Seeding season '$SeasonName' ($($SeasonStart.ToString('yyyy-MM-dd')) to $($SeasonEnd.ToString('yyyy-MM-dd')))..." -ForegroundColor Cyan

    # First season in an empty database, so the whole league goes in as
    # "promoted" and nothing is relegated. The handler carries the roster
    # forward from the previous season in later years, so this shape is only
    # right the first time.
    $seed = Invoke-Api -Path 'seednewseason' -Body @{
        seasonName    = $SeasonName
        startDate     = $SeasonStart.ToString('yyyy-MM-dd')
        endDate       = $SeasonEnd.ToString('yyyy-MM-dd')
        promotedTeams = @($board.Team)
        relegatedTeams = @()
    }

    Write-Host "  season      $($seed.seasonName) ($($seed.startYear))"
    Write-Host "  gameweeks   $($seed.gameweeksCreated)"
    Write-Host "  clubs new   $($seed.teamsCreated.Count)"
    Write-Host "  enrolled    $($seed.teamsEnrolled.Count)"
}

if (-not $TeamsOnly) {
    $date = $ValueDate.ToString('yyyy-MM-dd')
    Write-Host "Loading $($board.Count) prices for $date..." -ForegroundColor Cyan

    # Upserts on (TeamId, ValueDate), so re-running with corrected numbers
    # overwrites rather than duplicating or failing.
    $prices = Invoke-Api -Path 'prices/bulk' -Body @{
        valueDate = $date
        prices    = @($board | ForEach-Object {
            @{ teamName = $_.Team; bid = $_.Bid; ask = $_.Ask }
        })
    }

    Write-Host "  wrote $($prices.Count) prices"
    $prices |
        Sort-Object -Property mid -Descending |
        Format-Table -AutoSize teamName, bid, ask, mid
}

Write-Host "Done." -ForegroundColor Green
