#!/usr/bin/env perl
use strict;
use warnings;

use JSON;
use REST::Client;
use Time::HiRes qw( gettimeofday tv_interval );

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

my $pg_uuid = 'dd649268-d27d-4c6f-9ab8-ce6062b70276';

sub usage () {
    print STDERR "Usage: match-face.pl <photo>\n";
}

sub recognize_faces_from_file ($) {
    my $filedata = shift;

    my $url = '/detect'.
        '?returnFaceId=true'.
        '&returnFaceLandmarks=false'.
        '&returnFaceAttributes='.
        '&recognitionModel=recognition_02'.
        '';
    # my $data = '{"url":"https://upload.wikimedia.org/wikipedia/commons/f/fc/Ibukun_Odusote_%283347121979%29.jpg"}';
    my $data = $filedata;
    my $resp = $client->POST($url, $data, {'Content-Type' => 'application/octet-stream'});
    my $parsed_data = length($resp->responseContent()) > 0 ? decode_json($resp->responseContent()) : undef;
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
    # print "JSON:\n" . to_json($parsed_data, {utf8 => 1, pretty => 1}) . "\n";
    return $parsed_data;
    # return 1;
}

sub identify_faces ($) {
    my $faces = shift;
    my @faceIds;
    foreach my $face (@{$faces}) {
        push(@faceIds, ${$face}{'faceId'});
    }

    my $url = '/identify';
    my %data_obj = ( 'personGroupId' => $pg_uuid, 'faceIds' => \@faceIds );
    my $data = encode_json(\%data_obj);
    print "Json Data: $data\n";
    my $resp = $client->POST($url, $data, {'Content-Type' => 'application/json'});
    my $parsed_data = length($resp->responseContent()) > 0 ? decode_json($resp->responseContent()) : undef;
    unless ($resp->responseCode() eq '200') {
        print "WARN: identify_faces API call failed\n";
        print "Resp Code: " . $resp->responseCode() . "\n";
        print "Resp Data:\n" . to_json($parsed_data, {utf8 => 1, pretty => 1}) . "\n";
        return undef;
    }
    # return $parsed_data;
    # print "Resp Code: " . $resp->responseCode() . "\n";
    # print "Resp Data: " . $resp->responseContent() . "\n";
    # return undef unless $resp->responseCode() eq '200';
    # print "Faces detected: " . scalar(@{$parsed_data}) . "\n";
    print "JSON:\n" . to_json($parsed_data, {utf8 => 1, pretty => 1}) . "\n";
    return $parsed_data;
}

my $file = shift;
defined $file or usage and die "ERROR: Please pass a photo";

print "File: $file\n";

open(my $fh, '<:raw', $file) or die "ERROR: Unable to open file '$file'";
my $filedata;
(read($fh, $filedata, (-s $file)) == (-s $file)) or die "ERROR: Unable to read file '$file'";

my $starttime = [gettimeofday];
my $faces = recognize_faces_from_file($filedata) or die "ERROR: Unable to detect faces";
my $identifications = identify_faces($faces) or die "ERROR: Unable to identify faces";
my $elapsed = tv_interval($starttime);
print "Elapsed time to detect faces: $elapsed\n";


