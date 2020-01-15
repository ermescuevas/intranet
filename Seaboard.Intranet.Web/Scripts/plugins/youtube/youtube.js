// Define some variables used to remember state.
var playlistId, nextPageToken, prevPageToken;

googleApiClientReady = function () {
    loadAPIClientInterfaces();
}

function loadAPIClientInterfaces() {
    gapi.client.load('youtube', 'v3', function () {
        handleAPILoaded();
    });
}

//After the API loads, call a function to get the uploads playlist ID.
function handleAPILoaded() {
    requestUserUploadsPlaylistId();
}

//Call the Data API to retrieve the playlist ID that uniquely identifies the
//list of videos uploaded to the currently authenticated user's channel.
function requestUserUploadsPlaylistId() {
    //	See https://developers.google.com/youtube/v3/docs/channels/list
    var request = gapi.client.youtube.channels.list({
        forUsername: 'donostiakoudala',
        part: 'contentDetails',
        key: 'AIzaSyD_-22USYeYocPvX2y11snGZ1P4bF5cHRk'
    });

    request.execute(function (response) {
        //console.log(response);
        playlistId = response.result.items[0].contentDetails.relatedPlaylists.uploads;
        //console.log(playlistId);
        requestVideoPlaylist(playlistId);
    });
}

//Retrieve the list of videos in the specified playlist.
function requestVideoPlaylist(playlistId, pageToken) {
    $('#video-container').html('');
    var requestOptions = {
        playlistId: playlistId,
        part: 'snippet',
        maxResults: 4,
        key: 'AIzaSyD_-22USYeYocPvX2y11snGZ1P4bF5cHRk'
    };

    if (pageToken) {
        requestOptions.pageToken = pageToken;
    }

    var request = gapi.client.youtube.playlistItems.list(requestOptions);

    request.execute(function (response) {
        // Only show pagination buttons if there is a pagination token for the
        // next or previous page of results.
        nextPageToken = response.result.nextPageToken;
        var nextVis = nextPageToken ? 'visible' : 'hidden';
        //$('#next-button').css('visibility', nextVis);

        prevPageToken = response.result.prevPageToken
        var prevVis = prevPageToken ? 'visible' : 'hidden';
        //$('#prev-button').css('visibility', prevVis);

        var playlistItems = response.result.items;
        //console.log(response);
        if (playlistItems) {
            //$('#video-container').append('<h3>Videos mÃ¡s recientes</h3>');

            $.each(playlistItems, function (index, item) {
                displayResult(item.snippet);
            });

            //$('#video-container').append('</ul>');
        } else {
            $('#video-container').html('Sorry you have no uploaded videos');
        }
    });
}

//Create a listing for a video.
function displayResult(videoSnippet) {
    //console.log(videoSnippet);
    var title = videoSnippet.title;
    var videoId = videoSnippet.resourceId.videoId;
    var img = videoSnippet.thumbnails["medium"].url;
    var publishedAt = videoSnippet.publishedAt;
    var channelTitle = videoSnippet.channelTitle;

    $('#video-container').append('<div class="span3 thumbnail-youtube"><a href="https://www.youtube.com/watch?v=' + videoId + '"><img src="' + img + '"/></a><h4><a href="https://www.youtube.com/watch?v=' + videoId + '">' + title + '</a></h4></div>');
}