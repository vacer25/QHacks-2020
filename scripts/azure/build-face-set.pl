#!/usr/bin/env perl
use strict;
use warnings;

use JSON;
use REST::Client;

# Set the FACE_SUBSCRIPTION_KEY environment variable with your key as the value.
# This key will serve all examples in this document.
my $KEY = $ENV{'FACE_SUBSCRIPTION_KEY'};

# Set the FACE_ENDPOINT environment variable with the endpoint from your Face service in Azure.
# This endpoint will be used in all examples in this quickstart.
my $ENDPOINT = $ENV{'FACE_ENDPOINT'};

my $client = REST::Client->new();
$client->setHost($ENDPOINT);
$client->addHeader('Ocp-Apim-Subscription-Key', $KEY);

# At this point, the client should have all the API information needed.

sub usage () {
    print STDERR "Usage: build-face-set.pl <folder>\n";
}

sub read_meta($) {
    my $folder = shift;
    my $file = $folder . '/meta.json';
    open(my $fh, '<', $file) or die "ERROR: Unable to open meta file '$file'";
    (read $fh, my $jsondata, (-s $fh)) == (-s $fh) or die "ERROR: Unable to read meta file '$file'";
    my $parsed_data = decode_json($jsondata);
    my %pg = %{${$parsed_data}{'persongroup'}};
    my %person = %{${$parsed_data}{'person'}};
    return ($pg{'uuid'}, $pg{'name'}, $person{'name'});
}

sub parse_rect_dict ($) {
    my $rect = shift;
    my $left = ${$rect}{'left'};
    my $top = ${$rect}{'top'};
    my $width = ${$rect}{'width'};
    my $height = ${$rect}{'height'};
    return "$left,$top,$width,$height";
}

sub persongroup_get ($) {
    my $group_uuid = shift;
    defined $group_uuid or die "ERROR: Cannot create PersonGroup without group UUID";
    # https://westus.api.cognitive.microsoft.com/face/v1.0/persongroups/{personGroupId}?returnRecognitionModel=false
    my $url = '/persongroups/'.$group_uuid.'?returnRecognitionModel=true';
    my $resp = $client->GET($url);
    my $parsed_data = decode_json($resp->responseContent());
    unless ($resp->responseCode() eq '200') {
        print "WARN: persongroup_get API call failed\n";
        print "Resp Code: " . $resp->responseCode() . "\n";
        print "Resp Data:\n" . to_json($parsed_data, {utf8 => 1, pretty => 1}) . "\n";
        return undef;
    }
    return decode_json($resp->responseContent());
}

sub persongroup_create ($$) {
    my $group_uuid = shift;
    defined $group_uuid or die "ERROR: Cannot create PersonGroup without group UUID";
    my $group_name = shift;
    defined $group_name or die "ERROR: Cannot create PersonGroup without group name";
    print "Creating PersonGroup with name '$group_name'...\n";
    my $url = '/persongroups/' . $group_uuid;
    my $data = '{"recognitionModel":"recognition_02","name":"'.$group_name.'"}';
    my $resp = $client->PUT($url, $data, {'Content-Type' => 'application/json'});
    my $parsed_data = length($resp->responseContent()) > 0 ? decode_json($resp->responseContent()) : undef;
    if ($resp->responseCode() eq '409') {
        if (${${$parsed_data}{'error'}}{'code'} eq 'PersonGroupExists') {
            print "PersonGroup already existed.\n";
            return 1;
        }
    }
    unless ($resp->responseCode() eq '200') {
        print "WARN: persongroup_create API call failed\n";
        print "Resp Code: " . $resp->responseCode() . "\n";
        print "Resp Data:\n" . to_json($parsed_data, {utf8 => 1, pretty => 1}) . "\n";
        return undef;
    }
    return 1;
}

sub person_create ($$) {
    my $group_uuid = shift;
    defined $group_uuid or die "ERROR: Cannot create Person without group UUID";
    # my $p_uuid = shift;
    # defined $p_uuid or die "ERROR: Cannot create Person without person UUID";
    my $p_name = shift;
    defined $p_name or die "ERROR: Cannot create Person without person name";
    print "Creating Person with name '$p_name'...\n";
    my $url = '/persongroups/' . $group_uuid . '/persons';
    my $data = '{"name":"'.$p_name.'"}';
    my $resp = $client->POST($url, $data, {'Content-Type' => 'application/json'});
    my $parsed_data = length($resp->responseContent()) > 0 ? decode_json($resp->responseContent()) : undef;
    unless ($resp->responseCode() eq '200') {
        print "WARN: person_create API call failed\n";
        print "Resp Code: " . $resp->responseCode() . "\n";
        print "Resp Data:\n" . to_json($parsed_data, {utf8 => 1, pretty => 1}) . "\n";
        return undef;
    }
    my $personId = ${$parsed_data}{'personId'};
    print "Created new Person with id = '$personId'.\n";
    return $personId;
}

sub recognize_faces_from_file ($) {
    my $file = shift;
    open(my $fh, '<:raw', $file) or die "ERROR: Unable to open file '$file'";
    my $filedata;
    (read($fh, $filedata, (-s $file)) == (-s $file)) or die "ERROR: Unable to read file '$file'";

    my $url = '/detect'.
        '?returnFaceId=true'.
        '&returnFaceLandmarks=false'.
        '&returnFaceAttributes='.
        '&recognitionModel=recognition_02'.
        '';
    # my $data = '{"url":"https://upload.wikimedia.org/wikipedia/commons/f/fc/Ibukun_Odusote_%283347121979%29.jpg"}';
    my $data = $filedata;
    my $resp = $client->POST($url, $data, {'Content-Type' => 'application/octet-stream'});
    my $parsed_data = decode_json($resp->responseContent());
    unless ($resp->responseCode() eq '200') {
        print "WARN: recognize_faces_from_file API call failed\n";
        print "Resp Code: " . $resp->responseCode() . "\n";
        print "Resp Data:\n" . to_json($parsed_data, {utf8 => 1, pretty => 1}) . "\n";
        return undef;
    }
    # return $parsed_data;
    # print "Resp Code: " . $resp->responseCode() . "\n";
    # print "Resp Data: " . $resp->responseContent() . "\n";
    # return undef unless $resp->responseCode() eq '200';
    # my $parsed_data = decode_json($resp->responseContent());
    # print "Faces detected: " . scalar(@{$parsed_data}) . "\n";
    print "JSON:\n" . to_json($parsed_data, {utf8 => 1, pretty => 1}) . "\n";
    return $parsed_data;
}

sub person_add_face_from_file ($$$$) {
    my $pg_uuid = shift;
    defined $pg_uuid or die "ERROR: Cannot add face to Person without group ID";
    my $p_uuid = shift;
    defined $p_uuid or die "ERROR: Cannot add face to Person without ID";
    my $file = shift;
    open(my $fh, '<:raw', $file) or die "ERROR: Unable to open file '$file'";
    my $filedata;
    (read($fh, $filedata, (-s $file)) == (-s $file)) or die "ERROR: Unable to read file '$file'";
    my $rect = shift;

    my $url = '/persongroups/'.$pg_uuid.'/persons/'.$p_uuid.'/persistedFaces'.
        '?detectionModel=detection_02'.
        (defined($rect) ? '&targetFace='.$rect : '').
        '';
    # my $data = '{"url":"https://upload.wikimedia.org/wikipedia/commons/f/fc/Ibukun_Odusote_%283347121979%29.jpg"}';
    my $data = $filedata;
    my $resp = $client->POST($url, $data, {'Content-Type' => 'application/octet-stream'});
    my $parsed_data = length($resp->responseContent()) > 0 ? decode_json($resp->responseContent()) : undef;
    unless ($resp->responseCode() eq '200') {
        print "WARN: person_add_face_from_file API call failed\n";
        print "Resp Code: " . $resp->responseCode() . "\n";
        print "Resp Data:\n" . to_json($parsed_data, {utf8 => 1, pretty => 1}) . "\n";
        return undef;
    }
    return ${$parsed_data}{'persistedFaceId'};
}

my $folder = shift;
defined $folder or usage and die "ERROR: Please provide a name";
defined $folder or usage and die "ERROR: Please pass a folder to train";

print "Folder: $folder\n";

opendir(my $dirref, $folder) or die "ERROR: Unable to opendir";

my ($pg_uuid, $pg_name, $person_name) = read_meta($folder);
print "PG:     name = '$pg_name', uuid = '$pg_uuid'\n";
print "Person: name = '$person_name'\n";

# my $pg = persongroup_get($pg_uuid);
# unless (defined $pg) {
#     # Create PersonGroup
#     my $group_id = persongroup_create($pg_uuid, $pg_name);
#     $pg = persongroup_get($pg_uuid);
# }
persongroup_create($pg_uuid, $pg_name) or die "ERROR: Unable to create PersonGroup";
my $pg_data = persongroup_get($pg_uuid);
print "PG JSON:\n" . to_json($pg_data, {utf8 => 1, pretty => 1}) . "\n";

my $person_uuid = person_create($pg_uuid, $person_name) or die "ERROR: Unable to create Person";

while (my $file = readdir($dirref)) {
    next unless $file =~ m/^.*\.(jpg|png|gif|bmp)/;
    my $path = "$folder/$file";
    print "---------------------------------------------------------------------\nFile: $path\n";
    # Call Azure to get num of faces
    my $faces = recognize_faces_from_file($path) or die "ERROR: Unable to detect faces";
    if (scalar(@{$faces}) != 1) {
        print "WARNING: File has ".scalar(@{$faces})." faces (not 1). Skipping...\n";
        next;
    }
    my $rect_short = parse_rect_dict(${${$faces}[0]}{'faceRectangle'});
    # Assign to Person
    person_add_face_from_file($pg_uuid, $person_uuid, $path, $rect_short) or die "ERROR: Unable to add face";
}

