COMPILE_TARGET = ENV['config'].nil? ? "Debug" : ENV['config'] # Keep this in sync w/ VS settings since Mono is case-sensitive
CLR_TOOLS_VERSION = "v4.0.30319"

buildsupportfiles = Dir["#{File.dirname(__FILE__)}/buildsupport/*.rb"]

if( ! buildsupportfiles.any? )
  # no buildsupport, let's go get it for them.
  sh 'git submodule update --init' unless buildsupportfiles.any?
  buildsupportfiles = Dir["#{File.dirname(__FILE__)}/buildsupport/*.rb"]
end

# nope, we still don't have buildsupport. Something went wrong.
raise "Run `git submodule update --init` to populate your buildsupport folder." unless buildsupportfiles.any?

buildsupportfiles.each { |ext| load ext }

include FileTest
require 'albacore'
gem 'rubyzip'
require 'zip/zip'
require 'zip/zipfilesystem'
load "VERSION.txt"

RESULTS_DIR = "results"
PRODUCT = "Milkman"
COPYRIGHT = 'Copyright 2008-2013 Dru Sellers, Jeremy D. Miller, et al. All rights reserved.';
COMMON_ASSEMBLY_INFO = 'src/CommonAssemblyInfo.cs';
BUILD_DIR = File.expand_path("build")
ARTIFACTS = File.expand_path("artifacts")

@teamcity_build_id = "bt24"
tc_build_number = ENV["BUILD_NUMBER"]
build_revision = tc_build_number || Time.new.strftime('5%H%M')
BUILD_NUMBER = "#{BUILD_VERSION}.#{build_revision}"

props = { :stage => BUILD_DIR, :artifacts => ARTIFACTS }

desc "**Default**, compiles and runs tests"
task :default => [:compile, :create_bottles, :unit_test]

desc "Full compile and test (same as 'default' task)"
task :full => [:default]

desc "Target used for the CI server"
task :ci => [:update_all_dependencies, :default, :history, :package]

desc "Target used for CI on Mono"
task :mono_ci => [:update_all_dependencies, :default, :mono_unit_test]

desc "Update the version information for the build"
assemblyinfo :version do |asm|
  asm_version = BUILD_NUMBER

  begin
    commit = `git log -1 --pretty=format:%H`
  rescue
    commit = "git unavailable"
  end
  puts "##teamcity[buildNumber '#{BUILD_NUMBER}']" unless tc_build_number.nil?
  puts "Version: #{BUILD_NUMBER}" if tc_build_number.nil?
  asm.trademark = commit
  asm.product_name = PRODUCT
  asm.description = BUILD_NUMBER
  asm.version = asm_version
  asm.file_version = BUILD_NUMBER
  asm.custom_attributes :AssemblyInformationalVersion => asm_version
  asm.copyright = COPYRIGHT
  asm.output_file = COMMON_ASSEMBLY_INFO
end

desc "Prepares the working directory for a new build"
task :clean => [:update_buildsupport] do

  FileUtils.rm_rf props[:stage]
    # work around nasty latency issue where folder still exists for a short while after it is removed
    waitfor { !exists?(props[:stage]) }
  Dir.mkdir props[:stage]

  Dir.mkdir props[:artifacts] unless exists?(props[:artifacts])
end

def waitfor(&block)
  checks = 0
  until block.call || checks >10
    sleep 0.5
    checks += 1
  end
  raise 'waitfor timeout expired' if checks > 10
end

desc "Compiles the app"
task :compile => [:restore_if_missing, :clean, :version, "docs:bottle"] do
  MSBuildRunner.compile :compilemode => COMPILE_TARGET, :solutionfile => 'src/Milkman.sln', :clrversion => CLR_TOOLS_VERSION
  target = COMPILE_TARGET.downcase
end

task :create_bottles => :compile do
  milk_dir = "src/milk/bin/#{COMPILE_TARGET}"
  
  sh "#{milk_dir}/milk.exe create-all --output build --target #{COMPILE_TARGET}"
  File.delete "build/milkman.zip" if File.exist? "build/milkman.zip"
  
  outer_file_list = FileList.new("#{milk_dir}/*.*") do |fl|
    fl.exclude("#{milk_dir}/*.xml")
    fl.exclude("#{milk_dir}/*vshost*")    
  end
  
  bin_folder_file_list = FileList.new("#{milk_dir}/*.dll", "#{milk_dir}/*.exe") do |fl|
    fl.exclude("#{milk_dir}/*vshost*")
    fl.exclude("#{milk_dir}/*Deployers*")    
  end
  
  Zip::ZipFile.open("build/milkman.zip", Zip:ZipFile::CREATE) do |zip|
    outer_file_list.to_a.each do |f|
      zip.add(f.sub("#{milk_dir}/",''), f)
    end
    
    admin = "#{milk_dir}/../../../packages/Microsoft.Web.Administration/lib/net20/Microsoft.Web.Administration.dll"
    zip.add("Microsoft.Web.Administration.dll", admin)
    
    bin_folder_file_list.to_a.each do |f|
      zip.add(f.sub("#{milk_dir}/",'bin/'), f)
    end
    zip.add("bin/Microsoft.Web.Administration.dll", admin)
  end  
end


desc "Runs unit tests"
task :test => [:unit_test]

desc "Runs unit tests"
task :unit_test => :compile do
  runner = NUnitRunner.new :compilemode => COMPILE_TARGET, :source => 'src', :platform => 'x86'
  runner.executeTests ['Milkman.Testing']
end

desc "Runs some of the unit tests for Mono"
task :mono_unit_test => [:unit_test]