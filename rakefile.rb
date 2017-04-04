COMPILE_TARGET = ENV['config'].nil? ? "debug" : ENV['config']
BUILD_VERSION = '100.0.0'
tc_build_number = ENV["BUILD_NUMBER"]
build_revision = tc_build_number || Time.new.strftime('5%H%M')
build_number = "#{BUILD_VERSION}.#{build_revision}"
BUILD_NUMBER = build_number 

desc 'Compile the code'
task :compile => [:clean, :version] do
  msbuild = '"C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe"'
  sh "#{msbuild} src/Milkman.sln /property:Configuration=#{COMPILE_TARGET} /v:m /t:rebuild /nr:False /maxcpucount:8"
end

task :create_bottles do
  require 'zip'

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
  
  Zip::File.open("build/milkman.zip", Zip::File::CREATE) do |zip|
    outer_file_list.to_a.each do |f|
      zip.add(f.sub("#{milk_dir}/",''), f)
    end
    
    bin_folder_file_list.to_a.each do |f|
      zip.add(f.sub("#{milk_dir}/",'bin/'), f)
    end
  end  
end

desc 'Run the unit tests'
task :test => [:compile, :fast_test] do
end

desc 'Run the unit tests without compile'
task :fast_test do
  sh "src/packages/NUnit.ConsoleRunner.3.6.1/tools/nunit3-console.exe src/Milkman.Testing/bin/#{COMPILE_TARGET}/Milkman.Testing.dll"
end

desc "Prepares the working directory for a new build"
task :clean do
  FileUtils.rm_rf 'artifacts'
  Dir.mkdir 'artifacts'
end

desc "Update the version information for the build"
task :version do
  asm_version = build_number
  
  begin
    commit = `git log -1 --pretty=format:%H`
  rescue
    commit = "git unavailable"
  end
  puts "##teamcity[buildNumber '#{build_number}']" unless tc_build_number.nil?
  puts "Version: #{build_number}" if tc_build_number.nil?
  
  options = {
    :description => 'Milkman',
    :product_name => 'Milkman',
    :copyright => 'Copyright 2008-2013 Dru Sellers, Jeremy D. Miller, et al. All rights reserved.',
    :trademark => commit,
    :version => asm_version,
    :file_version => build_number,
    :informational_version => asm_version
  }
  
  puts "Writing src/CommonAssemblyInfo.cs..."
  File.open('src/CommonAssemblyInfo.cs', 'w') do |file|
    file.write "using System.Reflection;\n"
    file.write "using System.Runtime.InteropServices;\n"
    file.write "[assembly: AssemblyDescription(\"#{options[:description]}\")]\n"
    file.write "[assembly: AssemblyProduct(\"#{options[:product_name]}\")]\n"
    file.write "[assembly: AssemblyCopyright(\"#{options[:copyright]}\")]\n"
    file.write "[assembly: AssemblyTrademark(\"#{options[:trademark]}\")]\n"
    file.write "[assembly: AssemblyVersion(\"#{options[:version]}\")]\n"
    file.write "[assembly: AssemblyFileVersion(\"#{options[:file_version]}\")]\n"
    file.write "[assembly: AssemblyInformationalVersion(\"#{options[:informational_version]}\")]\n"
  end
end